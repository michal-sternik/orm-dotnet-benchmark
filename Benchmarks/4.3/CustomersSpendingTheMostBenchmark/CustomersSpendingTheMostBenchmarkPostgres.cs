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
    public class CustomersSpendingTheMostBenchmarkPostgres : OrmBenchmarkBase
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
        public List<CustomerSpendingDto> Dapper_ORM()
        {
            using var conn = CreatePostgresConnection();
            var sql = @"
                SELECT 
                    c.customerid, 
                    a.city AS region, 
                    SUM(soh.totaldue) AS totalspent
                FROM sales.salesorderheader soh
                JOIN sales.customer c ON soh.customerid = c.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                GROUP BY c.customerid, a.city
                ORDER BY totalspent DESC;";
            return conn.Query<CustomerSpendingDto>(sql).ToList();
        }

        [Benchmark]
        public List<CustomerSpendingDto> RepoDb_ORM()
        {
            using var conn = CreatePostgresConnection();
            var sql = @"
                SELECT 
                    c.customerid, 
                    a.city AS region, 
                    SUM(soh.totaldue) AS totalspent
                FROM sales.salesorderheader soh
                JOIN sales.customer c ON soh.customerid = c.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                GROUP BY c.customerid, a.city
                ORDER BY totalspent DESC;";
            return conn.ExecuteQuery<CustomerSpendingDto>(sql).ToList();
        }
        [Benchmark]
        public List<CustomerSpendingDto> SqlSugar_ORM()
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
                SELECT 
                    c.customerid, 
                    a.city AS region, 
                    SUM(soh.totaldue) AS totalspent
                FROM sales.salesorderheader soh
                JOIN sales.customer c ON soh.customerid = c.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                GROUP BY c.customerid, a.city
                ORDER BY totalspent DESC";
            return _sqlSugarClient.Ado.SqlQuery<CustomerSpendingDto>(sql);
        }
        [Benchmark]
        public List<CustomerSpendingDto> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();
            var sql = @"
                SELECT 
                    c.customerid, 
                    a.city AS region, 
                    SUM(soh.totaldue) AS totalspent
                FROM sales.salesorderheader soh
                JOIN sales.customer c ON soh.customerid = c.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                GROUP BY c.customerid, a.city
                ORDER BY totalspent DESC";
            return db.SqlList<CustomerSpendingDto>(sql);
        }
        [Benchmark]
        public List<CustomerSpendingDto> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<CustomerSpendingDto>(@"
                SELECT 
                    c.customerid, 
                    a.city AS region, 
                    SUM(soh.totaldue) AS totalspent
                FROM sales.salesorderheader soh
                JOIN sales.customer c ON soh.customerid = c.customerid
                JOIN person.address a ON soh.billtoaddressid = a.addressid
                GROUP BY c.customerid, a.city
                ORDER BY totalspent DESC
            ").ToList();
        }
        [Benchmark]
        public List<CustomerSpendingDto> EFCore_ORM()
        {
            using var context = CreatePostgresContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return context.SalesOrderHeaders
                .Include(soh => soh.Customer)
                .Include(soh => soh.BillToAddress)
                .GroupBy(soh => new
                {
                    CustomerId = soh.Customer.CustomerId,
                    Region = soh.BillToAddress.City
                })
                .Select(g => new CustomerSpendingDto
                {
                    CustomerId = g.Key.CustomerId,
                    Region = g.Key.Region,
                    TotalSpent = (decimal)g.Sum(x => (double)x.TotalDue)
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToList();
        }
    }
}
