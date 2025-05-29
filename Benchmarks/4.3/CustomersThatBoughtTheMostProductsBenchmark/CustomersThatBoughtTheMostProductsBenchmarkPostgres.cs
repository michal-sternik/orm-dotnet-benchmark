using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using ServiceStack.OrmLite;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Benchmarks;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class CustomersThatBoughtTheMostProductsBenchmarkPostgres : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDb() => PostgresRepoDbMappingSetup.Init();

        [GlobalSetup(Target = nameof(OrmLite_Postgres_LinqStyle))]
        public void SetupOrmLite() => OrmLiteSchemaConfigurator.ConfigureMappings();

        [Benchmark]
        public List<CustomerProductCountDto> EFCore_Postgres()
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

        [Benchmark]
        public List<CustomerProductCountDto> Dapper_Postgres()
        {
            using var conn = CreatePostgresConnection();
            var sql = @"
                SELECT c.customerid, COUNT(sod.productid) AS productcount
                FROM sales.salesorderdetail sod
                JOIN sales.salesorderheader soh ON sod.salesorderid = soh.salesorderid
                JOIN sales.customer c ON soh.customerid = c.customerid
                GROUP BY c.customerid
                HAVING COUNT(sod.productid) > 10
                ORDER BY productcount DESC;";
            return conn.Query<CustomerProductCountDto>(sql).ToList();
        }

        [Benchmark]
        public List<CustomerProductCountDto> RepoDb_Postgres()
        {
            using var conn = CreatePostgresConnection();
            var sql = @"
                SELECT c.customerid, COUNT(sod.productid) AS productcount
                FROM sales.salesorderdetail sod
                JOIN sales.salesorderheader soh ON sod.salesorderid = soh.salesorderid
                JOIN sales.customer c ON soh.customerid = c.customerid
                GROUP BY c.customerid
                HAVING COUNT(sod.productid) > 10
                ORDER BY productcount DESC;";
            return conn.ExecuteQuery<CustomerProductCountDto>(sql).ToList();
        }

        [Benchmark]
        public List<CustomerProductCountDto> OrmLite_Postgres_LinqStyle()
        {
            using var db = CreateOrmLitePostgresConnection();

            var q = db.From<SalesOrderDetail>()
                .Join<SalesOrderDetail, SalesOrderHeader>((sod, soh) => sod.SalesOrderId == soh.SalesOrderId)
                .Join<SalesOrderHeader, Customer>((soh, c) => soh.CustomerId == c.CustomerId)
                .GroupBy<SalesOrderDetail, SalesOrderHeader, Customer>((sod, soh, c) => c.CustomerId)
                .Having("COUNT(salesorderdetail.productid) > 10")
                .Select("customer.customerid, COUNT(salesorderdetail.productid) AS productcount")
                .OrderByDescending("productcount");

            return db.Select<CustomerProductCountDto>(q);
        }
    }
}
