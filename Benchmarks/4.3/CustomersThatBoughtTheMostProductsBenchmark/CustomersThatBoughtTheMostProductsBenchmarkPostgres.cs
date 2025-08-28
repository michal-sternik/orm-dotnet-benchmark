using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using ServiceStack.OrmLite;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Benchmarks;
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class CustomersThatBoughtTheMostProductsBenchmarkPostgres : OrmBenchmarkBase
    {
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDb() => RepoDbSchemaConfigurator.Init();

        [GlobalSetup(Target = nameof(OrmLite_ORM))]
        public void SetupOrmLite() => OrmLiteSchemaConfigurator.ConfigureMappings();

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
        public List<CustomerProductCountDto> Dapper_ORM()
        {
            using var conn = CreatePostgresConnection();
            var sql = @"
                SELECT c.customerid AS ""CustomerId"", COUNT(sod.productid) AS ""ProductCount""
                FROM sales.salesorderdetail sod
                JOIN sales.salesorderheader soh ON sod.salesorderid = soh.salesorderid
                JOIN sales.customer c ON soh.customerid = c.customerid
                GROUP BY c.customerid
                HAVING COUNT(sod.productid) > 10
                ORDER BY ""ProductCount"" DESC;";
            return conn.Query<CustomerProductCountDto>(sql).ToList();
        }

        [Benchmark]
        public List<CustomerProductCountDto> RepoDb_ORM()
        {
            using var conn = CreatePostgresConnection();
            var sql = @"
                SELECT c.customerid AS ""CustomerId"", COUNT(sod.productid) AS ""ProductCount""
                FROM sales.salesorderdetail sod
                JOIN sales.salesorderheader soh ON sod.salesorderid = soh.salesorderid
                JOIN sales.customer c ON soh.customerid = c.customerid
                GROUP BY c.customerid
                HAVING COUNT(sod.productid) > 10
                ORDER BY ""ProductCount"" DESC;";
            return conn.ExecuteQuery<CustomerProductCountDto>(sql).ToList();
        }
        [Benchmark]
        public List<CustomerProductCountDto> SqlSugar_ORM()
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
                SELECT c.customerid AS ""CustomerId"", COUNT(sod.productid) AS ""ProductCount""
                FROM sales.salesorderdetail sod
                JOIN sales.salesorderheader soh ON sod.salesorderid = soh.salesorderid
                JOIN sales.customer c ON soh.customerid = c.customerid
                GROUP BY c.customerid
                HAVING COUNT(sod.productid) > 10
                ORDER BY ""ProductCount"" DESC";
            return _sqlSugarClient.Ado.SqlQuery<CustomerProductCountDto>(sql);
        }
        [Benchmark]
        public List<CustomerProductCountDto> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();

            var sql = @"
                SELECT c.customerid AS ""CustomerId"", COUNT(sod.productid) AS ""ProductCount""
                FROM sales.salesorderdetail sod
                JOIN sales.salesorderheader soh ON sod.salesorderid = soh.salesorderid
                JOIN sales.customer c ON soh.customerid = c.customerid
                GROUP BY c.customerid
                HAVING COUNT(sod.productid) > 10
                ORDER BY ""ProductCount"" DESC";

            return db.SqlList<CustomerProductCountDto>(sql);
        }
        [Benchmark]
        public List<CustomerProductCountDto> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<CustomerProductCountDto>(@"
                SELECT c.customerid AS ""CustomerId"", COUNT(sod.productid) AS ""ProductCount""
                FROM sales.salesorderdetail sod
                JOIN sales.salesorderheader soh ON sod.salesorderid = soh.salesorderid
                JOIN sales.customer c ON soh.customerid = c.customerid
                GROUP BY c.customerid
                HAVING COUNT(sod.productid) > 10
                ORDER BY ""ProductCount"" DESC
            ").ToList();
        }

        [Benchmark]
        public List<CustomerProductCountDto> EFCore_ORM()
        {
            using var context = CreatePostgresContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return (from sod in context.SalesOrderDetails
                    join soh in context.SalesOrderHeaders on sod.SalesOrderId equals soh.SalesOrderId
                    join c in context.Customers on soh.CustomerId equals c.CustomerId
                    group sod by c.CustomerId into g
                    where g.Count() > 10
                    orderby g.Count() descending
                    select new CustomerProductCountDto
                    {
                        CustomerId = g.Key,
                        ProductCount = g.Count()
                    }).ToList();
        }
    }
}
