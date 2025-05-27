using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Config;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Benchmarks;
using ServiceStack.OrmLite;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectCustomersWithOrdersBenchmarkPostgres : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDbPostgres()
        {
            PostgresRepoDbMappingSetup.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_Postgres_LinqStyle))]
        public void SetupOrmLiteMappings()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }


        [Benchmark]
        public List<CustomerWithOrdersDto> Dapper_Postgres()
        {
            using var connection = CreatePostgresConnection();
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
        [InvocationCount(1)]
        public List<CustomerWithOrdersDto> EFCore_Postgres()
        {
            using var context = CreatePostgresContext();
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
        public List<CustomerWithOrdersDto> RepoDb_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<CustomerWithOrdersDto>(
                @"SELECT c.CustomerID, a.AddressLine1, sp.Name AS StateProvince, p.FirstName, p.LastName
                  FROM Sales.Customer c 
                  JOIN Sales.SalesOrderHeader soh ON c.CustomerID = soh.CustomerID
                  JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                  JOIN Person.StateProvince sp ON a.StateProvinceID = sp.StateProvinceID
                  JOIN Person.Person p ON c.PersonID = p.BusinessEntityID")
                .ToList();
        }


        [Benchmark]
        public List<CustomerWithOrdersDto> OrmLite_Postgres_LinqStyle()
        {
            using var db = CreateOrmLitePostgresConnection();

            var q = db.From<Customer>()
                .Join<Customer, SalesOrderHeader>((c, soh) => c.CustomerId == soh.CustomerId)
                .Join<SalesOrderHeader, Address>((soh, a) => soh.BillToAddressId == a.AddressId)
                .Join<Address, StateProvince>((a, sp) => a.StateProvinceId == sp.StateProvinceId)
                .Join<Customer, Person>((c, p) => c.PersonId == p.BusinessEntityId)
                .Select(@"Sales.Customer.CustomerID, 
                  Person.Address.AddressLine1, 
                  Person.StateProvince.Name as StateProvince, 
                  Person.Person.FirstName, 
                  Person.Person.LastName");

            return db.Select<CustomerWithOrdersDto>(q);
        }

    }
}
