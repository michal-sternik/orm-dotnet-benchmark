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
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDbPostgres()
        {
            RepoDbSchemaConfigurator.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_ORM))]
        public void SetupOrmLiteMappings()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }

        private IFreeSql _freeSqlPostgres;

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }




        [GlobalSetup(Target = nameof(SqlSugar_ORM))]
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
        public List<CustomerWithOrdersDto> Dapper_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.Query<CustomerWithOrdersDto>(
                @"SELECT c.customerid, a.addressline1, sp.name AS stateprovince, p.firstname, p.lastname
                  FROM sales.customer c 
                  JOIN sales.salesorderheader soh ON c.customerid = soh.customerid
                  JOIN person.address a ON soh.billtoaddressid = a.addressid
                  JOIN person.stateprovince sp ON a.stateprovinceid = sp.stateprovinceid
                  JOIN person.person p ON c.personid = p.businessentityid")
                .ToList();
        }



        [Benchmark]
        public List<CustomerWithOrdersDto> RepoDb_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<CustomerWithOrdersDto>(
                @"SELECT c.customerid, a.addressline1, sp.name AS stateprovince, p.firstname, p.lastname
                  FROM sales.customer c 
                  JOIN sales.salesorderheader soh ON c.customerid = soh.customerid
                  JOIN person.address a ON soh.billtoaddressid = a.addressid
                  JOIN person.stateprovince sp ON a.stateprovinceid = sp.stateprovinceid
                  JOIN person.person p ON c.personid = p.businessentityid")
                .ToList();
        }
        [Benchmark]
        public List<CustomerWithOrdersDto> SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
            var sql = @"
                SELECT c.customerid   AS CustomerID,
                       a.addressline1,
                       sp.name        AS StateProvince,
                       p.firstname,
                       p.lastname
                FROM sales.customer c 
                JOIN sales.salesorderheader soh ON c.customerid = soh.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                JOIN person.stateprovince sp ON a.stateprovinceid = sp.stateprovinceid
                JOIN person.person p ON c.personid = p.businessentityid";
            return _sqlSugarClient.Ado.SqlQuery<CustomerWithOrdersDto>(sql);
        }

        [Benchmark]
        public List<CustomerWithOrdersDto> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();

            var sql = @"
                SELECT c.customerid, a.addressline1, sp.name AS stateprovince, p.firstname, p.lastname
                FROM sales.customer c 
                JOIN sales.salesorderheader soh ON c.customerid = soh.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                JOIN person.stateprovince sp ON a.stateprovinceid = sp.stateprovinceid
                JOIN person.person p ON c.personid = p.businessentityid";

            return db.SqlList<CustomerWithOrdersDto>(sql);
        }
        [Benchmark]
        public List<CustomerWithOrdersDto> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<CustomerWithOrdersDto>(@"
                SELECT c.customerid, a.addressline1, sp.name AS stateprovince, p.firstname, p.lastname
                FROM sales.customer c 
                JOIN sales.salesorderheader soh ON c.customerid = soh.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                JOIN person.stateprovince sp ON a.stateprovinceid = sp.stateprovinceid
                JOIN person.person p ON c.personid = p.businessentityid
            ").ToList();
        }
        [Benchmark]
      
        public List<CustomerWithOrdersDto> EFCore_ORM()
        {
            using var context = CreatePostgresContext();

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
}
