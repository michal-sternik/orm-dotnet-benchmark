using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Config;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Benchmarks;
using LinqToDB;
using ServiceStack.OrmLite;
using LinqToDB.Reflection;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectCustomersWithOrdersBenchmarkMssql : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_MSSQL))]
        public void SetupRepoDbMssql()
        {
            // Mapowania RepoDB jeśli potrzebne
            FluentMapper.Entity<Customer>().Table("Sales.Customer");
            FluentMapper.Entity<SalesOrderHeader>().Table("Sales.SalesOrderHeader");
            FluentMapper.Entity<Address>().Table("Person.Address");
            FluentMapper.Entity<StateProvince>().Table("Person.StateProvince");
            FluentMapper.Entity<Person>().Table("Person.Person");
        }




        //dla ormlite dla tych dwoch benchmarkow czas jest praktycznie identyczny.
        [Benchmark]
        public List<CustomerWithOrdersDto> OrmLite_MSSQL_LinqStyle()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var q = db.From<Customer>()
                .Join<Customer, SalesOrderHeader>((c, soh) => c.CustomerId == soh.CustomerId)
                .Join<SalesOrderHeader, Address>((soh, a) => soh.BillToAddressId == a.AddressId)
                .Join<Address, StateProvince>((a, sp) => a.StateProvinceId == sp.StateProvinceId)
                .Join<Customer, Person>((c, p) => c.PersonId == p.BusinessEntityId)
                .Select("Sales.Customer.CustomerID, Person.Address.AddressLine1, Person.StateProvince.Name as StateProvince, Person.Person.FirstName, Person.Person.LastName");


            return db.Select<CustomerWithOrdersDto>(q);

        }

        //czyli to raczej bedzie do wywalenia
        [Benchmark]
        public List<CustomerWithOrdersDto> OrmLite_MSSQL()
        {
            using var db = CreateOrmLiteMssqlConnection();

            // Wersja z prostym joinem przez SQL
            var sql = @"
                SELECT c.CustomerID, a.AddressLine1, sp.Name AS StateProvince, p.FirstName, p.LastName
                FROM Sales.Customer c 
                JOIN Sales.SalesOrderHeader soh ON c.CustomerID = soh.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                JOIN Person.StateProvince sp ON a.StateProvinceID = sp.StateProvinceID
                JOIN Person.Person p ON c.PersonID = p.BusinessEntityID";

            return db.SqlList<CustomerWithOrdersDto>(sql);
        }

        //a tutaj sobie porównamy tez roznice czasowe w efcore - czyli nie orm jest wolny tylko sposób uzycia orm moze byc zly
        [Benchmark]
        public List<CustomerWithOrdersDto> EFCore_MSSQL_IncludeStyle()
        {
            using var context = CreateMssqlContext();
            return context.Customers
                .Include(c => c.Person)
                .Include(c => c.SalesOrderHeaders)
                    .ThenInclude(soh => soh.BillToAddress)
                        .ThenInclude(addr => addr.StateProvince)
                .Select(c => new CustomerWithOrdersDto
                {
                    CustomerID = c.CustomerId,
                    AddressLine1 = c.SalesOrderHeaders.FirstOrDefault().BillToAddress.AddressLine1,
                    StateProvince = c.SalesOrderHeaders.FirstOrDefault().BillToAddress.StateProvince.Name,
                    FirstName = c.Person.FirstName,
                    LastName = c.Person.LastName
                })
                .ToList();
        }


        [Benchmark]
        public List<CustomerWithOrdersDto> EFCore_MSSQL_OptimizedProjectionWithNoTracking()
        {
            using var context = CreateMssqlContext();

            var result = context.Customers
                .AsNoTracking()
                .Select(c => new
                {
                    c.CustomerId,
                    c.Person.FirstName,
                    c.Person.LastName,
                    FirstOrder = c.SalesOrderHeaders
                        .OrderBy(soh => soh.OrderDate)
                        .Select(soh => new
                        {
                            soh.BillToAddress.AddressLine1,
                            StateProvince = soh.BillToAddress.StateProvince.Name
                        })
                        .FirstOrDefault()
                })
                .Where(c => c.FirstOrder != null)
                .Select(c => new CustomerWithOrdersDto
                {
                    CustomerID = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    AddressLine1 = c.FirstOrder.AddressLine1,
                    StateProvince = c.FirstOrder.StateProvince
                })
                .ToList();

            return result;
        }

        [Benchmark]
        public List<CustomerWithOrdersDto> EFCore_MSSQL_OptimizedProjection()
        {
            using var context = CreateMssqlContext();

            var result = context.Customers
                .Select(c => new
                {
                    c.CustomerId,
                    c.Person.FirstName,
                    c.Person.LastName,
                    FirstOrder = c.SalesOrderHeaders
                        .OrderBy(soh => soh.OrderDate)
                        .Select(soh => new
                        {
                            soh.BillToAddress.AddressLine1,
                            StateProvince = soh.BillToAddress.StateProvince.Name
                        })
                        .FirstOrDefault()
                })
                .Where(c => c.FirstOrder != null)
                .Select(c => new CustomerWithOrdersDto
                {
                    CustomerID = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    AddressLine1 = c.FirstOrder.AddressLine1,
                    StateProvince = c.FirstOrder.StateProvince
                })
                .ToList();

            return result;
        }


        [Benchmark]
        public List<CustomerWithOrdersDto> EfCore_MSSQL_WithJoins()
        {
            using var context = CreateMssqlContext();

            return (from c in context.Customers
                    join soh in context.SalesOrderHeaders on c.CustomerId equals soh.CustomerId
                    join a in context.Addresses on soh.BillToAddressId equals a.AddressId
                    join sp in context.StateProvinces on a.StateProvinceId equals sp.StateProvinceId
                    join p in context.People on c.PersonId equals p.BusinessEntityId
                    select new CustomerWithOrdersDto
                    {
                        CustomerID = c.CustomerId,
                        AddressLine1 = a.AddressLine1,
                        StateProvince = sp.Name,
                        FirstName = p.FirstName,
                        LastName = p.LastName
                    }).ToList();
        }


        [Benchmark]
        public List<CustomerWithOrdersDto> Dapper_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<CustomerWithOrdersDto>(
                @"SELECT c.CustomerID, a.AddressLine1, sp.Name AS StateProvince, p.FirstName, p.LastName
                  FROM Sales.Customer c 
                  JOIN Sales.SalesOrderHeader soh ON c.CustomerID = soh.CustomerID
                  JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                  JOIN Person.StateProvince sp ON a.StateProvinceID = sp.StateProvinceID
                  JOIN Person.Person p ON c.PersonID = p.BusinessEntityID")
                .ToList();
        }

        [Benchmark]
        public List<CustomerWithOrdersDto> RepoDb_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.ExecuteQuery<CustomerWithOrdersDto>(
                @"SELECT c.CustomerID, a.AddressLine1, sp.Name AS StateProvince, p.FirstName, p.LastName
                  FROM Sales.Customer c 
                  JOIN Sales.SalesOrderHeader soh ON c.CustomerID = soh.CustomerID
                  JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                  JOIN Person.StateProvince sp ON a.StateProvinceID = sp.StateProvinceID
                  JOIN Person.Person p ON c.PersonID = p.BusinessEntityID")
                .ToList();
        }



    }


    public class CustomerWithOrdersDto
        {
            public int CustomerID { get; set; }
            public string AddressLine1 { get; set; }
            public string StateProvince { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
    }
    

}


//| Method | Mean | Error | StdDev | Allocated |
//| ------------------------ | ----------:| ----------:| ----------:| ----------:|
//| OrmLite_MSSQL_LinqStyle | 90.25 ms | 3.364 ms | 2.002 ms | 8.34 MB |
//| OrmLite_MSSQL | 89.05 ms | 4.204 ms | 2.781 ms | 8.32 MB |
//| EFCore_MSSQL | 606.15 ms | 5.994 ms | 3.567 ms | 8.58 MB |
//| EfCore_WithJoins_MSSQL | 96.26 ms | 2.467 ms | 1.468 ms | 13.47 MB |
//| Dapper_MSSQL | 106.41 ms | 20.297 ms | 13.425 ms | 8.56 MB |
//| RepoDb_MSSQL | 93.68 ms | 7.193 ms | 4.758 ms | 7.84 MB |

//| Method | Mean | Error | StdDev | Median | Allocated |
//| -------------------------- | ----------:| -----------:| ----------:| ----------:| ----------:|
//| OrmLite_MSSQL_LinqStyle | 93.58 ms | 5.466 ms | 3.615 ms | 92.63 ms | 8.34 MB |
//| OrmLite_MSSQL | 90.88 ms | 1.617 ms | 0.846 ms | 90.94 ms | 8.32 MB |
//| EFCore_MSSQL_IncludeStyle | 631.69 ms | 10.598 ms | 7.010 ms | 630.58 ms | 8.58 MB |
//| EfCore_MSSQL_WithJoins | 158.29 ms | 124.915 ms | 82.623 ms | 116.52 ms | 13.47 MB |
//| Dapper_MSSQL | 99.13 ms | 5.632 ms | 3.725 ms | 98.61 ms | 8.56 MB |
//| RepoDb_MSSQL | 98.89 ms | 10.428 ms | 6.897 ms | 97.90 ms | 7.84 MB |

//| Method | Mean | Error | StdDev | Allocated |
//| ----------------------------------------------- | ----------:| ----------:| ---------:| ----------:|
//| OrmLite_MSSQL_LinqStyle | 89.61 ms | 1.769 ms | 0.925 ms | 8.34 MB |
//| OrmLite_MSSQL | 89.22 ms | 5.882 ms | 3.891 ms | 8.32 MB |
//| EFCore_MSSQL_IncludeStyle | 624.62 ms | 7.574 ms | 5.009 ms | 8.58 MB |
//| EFCore_MSSQL_OptimizedProjectionWithNoTracking | 821.96 ms | 7.500 ms | 4.960 ms | 8.42 MB |
//| EFCore_MSSQL_OptimizedProjection | 818.58 ms | 10.162 ms | 6.722 ms | 8.42 MB |
//| EfCore_MSSQL_WithJoins | 96.56 ms | 3.950 ms | 2.613 ms | 13.47 MB |
//| Dapper_MSSQL | 91.93 ms | 1.933 ms | 1.279 ms | 8.56 MB |
//| RepoDb_MSSQL | 90.06 ms | 3.271 ms | 2.164 ms | 7.84 MB |