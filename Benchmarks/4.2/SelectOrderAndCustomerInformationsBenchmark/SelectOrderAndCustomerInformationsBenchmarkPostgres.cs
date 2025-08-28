using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Config;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServiceStack.OrmLite;
using OrmBenchmarkMag.Benchmarks;
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectOrderAndCustomerInformationsBenchmarkPostgres : OrmBenchmarkBase
    {
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
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

  

        private SqlSugarClient _sqlSugarClient;

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
        public List<OrderProductDetailDto> Dapper_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.Query<OrderProductDetailDto>(@"
                SELECT soh.salesorderid,
                       p.name AS productname,
                       sod.orderqty,
                       pe.firstname,
                       pe.lastname,
                       c.accountnumber,
                       a.city
                FROM sales.salesorderheader soh
                JOIN sales.salesorderdetail sod ON soh.salesorderid = sod.salesorderid
                JOIN production.product p ON sod.productid = p.productid
                JOIN sales.customer c ON soh.customerid = c.customerid
                LEFT JOIN person.person pe ON c.customerid = pe.businessentityid
                JOIN person.address a ON soh.shiptoaddressid = a.addressid
                ORDER BY soh.salesorderid").ToList();
        }

        [Benchmark]
        public List<OrderProductDetailDto> RepoDb_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<OrderProductDetailDto>(@"
                SELECT soh.salesorderid,
                       p.name AS productname,
                       sod.orderqty,
                       pe.firstname,
                       pe.lastname,
                       c.accountnumber,
                       a.city
                FROM sales.salesorderheader soh
                JOIN sales.salesorderdetail sod ON soh.salesorderid = sod.salesorderid
                JOIN production.product p ON sod.productid = p.productid
                JOIN sales.customer c ON soh.customerid = c.customerid
                LEFT JOIN person.person pe ON c.customerid = pe.businessentityid
                JOIN person.address a ON soh.shiptoaddressid = a.addressid
                ORDER BY soh.salesorderid").ToList();
        }
        [Benchmark]
        public List<OrderProductDetailDto> SqlSugar_ORM()
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
                SELECT soh.salesorderid,
                       p.name AS productname,
                       sod.orderqty,
                       pe.firstname,
                       pe.lastname,
                       c.accountnumber,
                       a.city
                FROM sales.salesorderheader soh
                JOIN sales.salesorderdetail sod ON soh.salesorderid = sod.salesorderid
                JOIN production.product p ON sod.productid = p.productid
                JOIN sales.customer c ON soh.customerid = c.customerid
                LEFT JOIN person.person pe ON c.customerid = pe.businessentityid
                JOIN person.address a ON soh.shiptoaddressid = a.addressid
                ORDER BY soh.salesorderid";
            return _sqlSugarClient.Ado.SqlQuery<OrderProductDetailDto>(sql);
        }
        [Benchmark]
        public List<OrderProductDetailDto> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();
            var sql = @"
                SELECT soh.salesorderid,
                       p.name AS productname,
                       sod.orderqty,
                       pe.firstname,
                       pe.lastname,
                       c.accountnumber,
                       a.city
                FROM sales.salesorderheader soh
                JOIN sales.salesorderdetail sod ON soh.salesorderid = sod.salesorderid
                JOIN production.product p ON sod.productid = p.productid
                JOIN sales.customer c ON soh.customerid = c.customerid
                LEFT JOIN person.person pe ON c.customerid = pe.businessentityid
                JOIN person.address a ON soh.shiptoaddressid = a.addressid
                ORDER BY soh.salesorderid";
            return db.SqlList<OrderProductDetailDto>(sql);
        }
        [Benchmark]
        public List<OrderProductDetailDto> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<OrderProductDetailDto>(@"
                SELECT soh.salesorderid,
                        p.name AS productname,
                        sod.orderqty,
                        pe.firstname,
                        pe.lastname,
                        c.accountnumber,
                        a.city
                FROM sales.salesorderheader soh
                JOIN sales.salesorderdetail sod ON soh.salesorderid = sod.salesorderid
                JOIN production.product p ON sod.productid = p.productid
                JOIN sales.customer c ON soh.customerid = c.customerid
                LEFT JOIN person.person pe ON c.customerid = pe.businessentityid
                JOIN person.address a ON soh.shiptoaddressid = a.addressid
                ORDER BY soh.salesorderid
            ").ToList();
        }
        [Benchmark]
        public List<OrderProductDetailDto> EFCore_ORM()
        {
            using var context = CreatePostgresContext();
            return (from soh in context.SalesOrderHeaders
                    join sod in context.SalesOrderDetails on soh.SalesOrderId equals sod.SalesOrderId
                    join p in context.Products on sod.ProductId equals p.ProductId
                    join c in context.Customers on soh.CustomerId equals c.CustomerId
                    join a in context.Addresses on soh.ShipToAddressId equals a.AddressId
                    join pe in context.People on c.CustomerId equals pe.BusinessEntityId into peJoin
                    from pe in peJoin.DefaultIfEmpty()
                    orderby soh.SalesOrderId
                    select new OrderProductDetailDto
                    {
                        SalesOrderId = soh.SalesOrderId,
                        ProductName = p.Name,
                        OrderQty = sod.OrderQty,
                        FirstName = pe.FirstName,
                        LastName = pe.LastName,
                        AccountNumber = c.AccountNumber,
                        City = a.City
                    }).ToList();
        }
    }
}
