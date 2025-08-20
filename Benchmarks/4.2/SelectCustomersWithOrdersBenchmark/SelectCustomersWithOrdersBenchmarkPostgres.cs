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
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectCustomersWithOrdersBenchmarkPostgres : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDbPostgres()
        {
            RepoDbSchemaConfigurator.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_Postgres_LinqStyle))]
        public void SetupOrmLiteMappings()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }

        private IFreeSql _freeSqlPostgres;

        [GlobalSetup(Target = nameof(FreeSql_Postgres))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        [Benchmark]
        public List<CustomerWithOrdersDto> FreeSql_Postgres()
        {
            return _freeSqlPostgres.Ado.Query<CustomerWithOrdersDto>(@"
                SELECT c.customerid, a.addressline1, sp.name AS stateprovince, p.firstname, p.lastname
                FROM sales.customer c 
                JOIN sales.salesorderheader soh ON c.customerid = soh.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                JOIN person.stateprovince sp ON a.stateprovinceid = sp.stateprovinceid
                JOIN person.person p ON c.personid = p.businessentityid
            ").ToList();
        }


        [GlobalSetup(Target = nameof(SqlSugar_Postgres))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
        }

        [Benchmark]
        public List<CustomerWithOrdersDto> SqlSugar_Postgres()
        {
            var list = _sqlSugarClient.Queryable<Customer>()
                .LeftJoin<SalesOrderHeader>((c, soh) => c.CustomerId == soh.CustomerId)
                .LeftJoin<Address>((c, soh, a) => soh.BillToAddressId == a.AddressId)
                .LeftJoin<StateProvince>((c, soh, a, sp) => a.StateProvinceId == sp.StateProvinceId)
                .LeftJoin<Person>((c, soh, a, sp, p) => c.PersonId == p.BusinessEntityId)
                .Select((c, soh, a, sp, p) => new CustomerWithOrdersDto
                {
                    CustomerID = c.CustomerId,
                    AddressLine1 = a.AddressLine1,
                    StateProvince = sp.Name,
                    FirstName = p.FirstName,
                    LastName = p.LastName
                })
                .ToList();

            return list;
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

        //[Benchmark]
        ////to sie robi za dlugo
        //[InvocationCount(1)]
        //[WarmupCount(1)]
        //public List<CustomerWithOrdersDto> EFCore_Postgres()
        //{
        //    using var context = CreatePostgresContext();
        //    return context.Customers
        //        .Include(c => c.Person)
        //        .Include(c => c.SalesOrderHeaders)
        //            .ThenInclude(soh => soh.BillToAddress)
        //                .ThenInclude(addr => addr.StateProvince)
        //        .Select(c => new CustomerWithOrdersDto
        //        {
        //            CustomerID = c.CustomerId,
        //            AddressLine1 = c.SalesOrderHeaders.FirstOrDefault().BillToAddress.AddressLine1,
        //            StateProvince = c.SalesOrderHeaders.FirstOrDefault().BillToAddress.StateProvince.Name,
        //            FirstName = c.Person.FirstName,
        //            LastName = c.Person.LastName
        //        })
        //        .ToList();
        //}

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
