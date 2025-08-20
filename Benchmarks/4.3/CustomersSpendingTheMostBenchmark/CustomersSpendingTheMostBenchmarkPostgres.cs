using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using ServiceStack.OrmLite;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Benchmarks;
using ServiceStack;
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class CustomersSpendingTheMostBenchmarkPostgres : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDb() => RepoDbSchemaConfigurator.Init();

        [GlobalSetup(Target = nameof(OrmLite_Postgres_LinqStyle))]
        public void SetupOrmLite() => OrmLiteSchemaConfigurator.ConfigureMappings();

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
        public List<CustomerSpendingDto> FreeSql_Postgres()
        {
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


        private SqlSugarClient _sqlSugarClient;

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
        public List<CustomerSpendingDto> SqlSugar_Postgres()
        {
            var list = _sqlSugarClient.Queryable<SalesOrderHeader>()
                .LeftJoin<Customer>((soh, c) => soh.CustomerId == c.CustomerId)
                .LeftJoin<Address>((soh, c, a) => soh.BillToAddressId == a.AddressId)
                .GroupBy((soh, c, a) => new { c.CustomerId, a.City })
                .OrderBy((soh, c, a) => SqlFunc.AggregateSum(soh.TotalDue), OrderByType.Desc)
                .Select((soh, c, a) => new CustomerSpendingDto
                {
                    CustomerId = c.CustomerId,
                    Region = a.City,
                    TotalSpent = SqlFunc.AggregateSum(soh.TotalDue)
                })
                .ToList();

            return list;
        }


        [Benchmark]
        public List<CustomerSpendingDto> EFCore_Postgres()
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

        [Benchmark]
        public List<CustomerSpendingDto> Dapper_Postgres()
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
        public List<CustomerSpendingDto> RepoDb_Postgres()
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
        public List<CustomerSpendingDto> OrmLite_Postgres_LinqStyle()
        {
            using var db = CreateOrmLitePostgresConnection();

            var q = db.From<SalesOrderHeader>()
                .Join<SalesOrderHeader, Customer>((soh, c) => soh.CustomerId == c.CustomerId)
                .Join<SalesOrderHeader, Address>((soh, a) => soh.BillToAddressId == a.AddressId)
                .GroupBy<SalesOrderHeader, Customer, Address>((soh, c, a) => new { c.CustomerId, a.City })
                .Select<SalesOrderHeader, Customer, Address>((soh, c, a) => new
                {
                    CustomerId = c.CustomerId,
                    Region = a.City,
                    TotalSpent = Sql.Sum(soh.TotalDue)
                })
                .OrderByDescending("TotalSpent");

            return db.Select<CustomerSpendingDto>(q);
        }
    }
}
