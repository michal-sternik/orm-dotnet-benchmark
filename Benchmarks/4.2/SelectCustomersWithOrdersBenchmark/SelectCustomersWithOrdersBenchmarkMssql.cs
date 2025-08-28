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
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectCustomersWithOrdersBenchmarkMssql : OrmBenchmarkBase
    {
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(RepoDb))]
        public void SetupRepoDbMssql()
        {
            // Mapowania RepoDB jeśli potrzebne
            FluentMapper.Entity<Customer>().Table("Sales.Customer");
            FluentMapper.Entity<SalesOrderHeader>().Table("Sales.SalesOrderHeader");
            FluentMapper.Entity<Address>().Table("Person.Address");
            FluentMapper.Entity<StateProvince>().Table("Person.StateProvince");
            FluentMapper.Entity<Person>().Table("Person.Person");
        }

        private IFreeSql _freeSqlMssql;

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

  


        [GlobalSetup(Target = nameof(SqlSugar_ORM) )]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
        }

 



  


        [Benchmark]
        public List<CustomerWithOrdersDto> Dapper_ORM()
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
        public List<CustomerWithOrdersDto> RepoDb_ORM()
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

        [Benchmark]
        public List<CustomerWithOrdersDto> SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
            var sql = @"
                SELECT 
                    c.CustomerId   AS CustomerID,
                    a.AddressLine1,
                    sp.Name        AS StateProvince,
                    p.FirstName,
                    p.LastName
                FROM Sales.Customer c 
                JOIN Sales.SalesOrderHeader soh ON c.CustomerID = soh.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                JOIN Person.StateProvince sp ON a.StateProvinceID = sp.StateProvinceID
                JOIN Person.Person p ON c.PersonID = p.BusinessEntityID";
            var list = _sqlSugarClient.Ado.SqlQuery<CustomerWithOrdersDto>(sql);
            return list;
        }
        
        [Benchmark]
        public List<CustomerWithOrdersDto> OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();

            
            var sql = @"
                SELECT c.CustomerID, a.AddressLine1, sp.Name AS StateProvince, p.FirstName, p.LastName
                FROM Sales.Customer c 
                JOIN Sales.SalesOrderHeader soh ON c.CustomerID = soh.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                JOIN Person.StateProvince sp ON a.StateProvinceID = sp.StateProvinceID
                JOIN Person.Person p ON c.PersonID = p.BusinessEntityID";

            return db.SqlList<CustomerWithOrdersDto>(sql);
        }
        [Benchmark]
        public List<CustomerWithOrdersDto> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
             .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
             .UseAutoSyncStructure(false)
             .Build();
            return _freeSqlMssql.Ado.Query<CustomerWithOrdersDto>(@"
                SELECT c.CustomerID, a.AddressLine1, sp.Name AS StateProvince, p.FirstName, p.LastName
                FROM Sales.Customer c 
                JOIN Sales.SalesOrderHeader soh ON c.CustomerID = soh.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                JOIN Person.StateProvince sp ON a.StateProvinceID = sp.StateProvinceID
                JOIN Person.Person p ON c.PersonID = p.BusinessEntityID
            ").ToList();
        }
       
        [Benchmark]
        public List<CustomerWithOrdersDto> EFCore_ORM()
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
//| OrmLite_LinqStyle | 90.25 ms | 3.364 ms | 2.002 ms | 8.34 MB |
//| OrmLite | 89.05 ms | 4.204 ms | 2.781 ms | 8.32 MB |
//| EFCore | 606.15 ms | 5.994 ms | 3.567 ms | 8.58 MB |
//| EfCore_WithJoins | 96.26 ms | 2.467 ms | 1.468 ms | 13.47 MB |
//| Dapper | 106.41 ms | 20.297 ms | 13.425 ms | 8.56 MB |
//| RepoDb | 93.68 ms | 7.193 ms | 4.758 ms | 7.84 MB |

//| Method | Mean | Error | StdDev | Median | Allocated |
//| -------------------------- | ----------:| -----------:| ----------:| ----------:| ----------:|
//| OrmLite_LinqStyle | 93.58 ms | 5.466 ms | 3.615 ms | 92.63 ms | 8.34 MB |
//| OrmLite | 90.88 ms | 1.617 ms | 0.846 ms | 90.94 ms | 8.32 MB |
//| EFCore_IncludeStyle | 631.69 ms | 10.598 ms | 7.010 ms | 630.58 ms | 8.58 MB |
//| EfCore_WithJoins | 158.29 ms | 124.915 ms | 82.623 ms | 116.52 ms | 13.47 MB |
//| Dapper | 99.13 ms | 5.632 ms | 3.725 ms | 98.61 ms | 8.56 MB |
//| RepoDb | 98.89 ms | 10.428 ms | 6.897 ms | 97.90 ms | 7.84 MB |

//| Method | Mean | Error | StdDev | Allocated |
//| ----------------------------------------------- | ----------:| ----------:| ---------:| ----------:|
//| OrmLite_LinqStyle | 89.61 ms | 1.769 ms | 0.925 ms | 8.34 MB |
//| OrmLite | 89.22 ms | 5.882 ms | 3.891 ms | 8.32 MB |
//| EFCore_IncludeStyle | 624.62 ms | 7.574 ms | 5.009 ms | 8.58 MB |
//| EFCore_OptimizedProjectionWithNoTracking | 821.96 ms | 7.500 ms | 4.960 ms | 8.42 MB |
//| EFCore_OptimizedProjection | 818.58 ms | 10.162 ms | 6.722 ms | 8.42 MB |
//| EfCore_WithJoins | 96.56 ms | 3.950 ms | 2.613 ms | 13.47 MB |
//| Dapper | 91.93 ms | 1.933 ms | 1.279 ms | 8.56 MB |
//| RepoDb | 90.06 ms | 3.271 ms | 2.164 ms | 7.84 MB |