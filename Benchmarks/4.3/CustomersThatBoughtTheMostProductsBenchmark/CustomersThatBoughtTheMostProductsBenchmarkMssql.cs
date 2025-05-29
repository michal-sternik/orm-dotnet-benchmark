using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using ServiceStack.OrmLite;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;

namespace OrmBenchmarkMag.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class CustomersThatBoughtTheMostProductsBenchmarkMssql : OrmBenchmarkBase
    {

        [Benchmark]
        public List<CustomerProductCountDto> EFCore_MSSQL_WithJoins()
        {
            using var context = CreateMssqlContext();
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
        public List<CustomerProductCountDto> EFCore_MSSQL_WithIncludeAndGroup()
        {
            using var context = CreateMssqlContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return context.SalesOrderDetails
                .Include(sod => sod.SalesOrder)//salesorderheader
                    .ThenInclude(soh => soh.Customer)
                .GroupBy(sod => sod.SalesOrder.Customer.CustomerId)
                .Where(g => g.Count() > 10)
                .Select(g => new CustomerProductCountDto
                {
                    CustomerId = g.Key,
                    ProductCount = g.Count()
                })
                .OrderByDescending(x => x.ProductCount)
                .ToList();
        }

        [Benchmark]
        public List<CustomerProductCountDto> Dapper_MSSQL()
        {
            using var conn = CreateMssqlConnection();
            var sql = @"
                SELECT c.CustomerID, COUNT(sod.ProductID) AS ProductCount
                FROM Sales.SalesOrderDetail sod
                JOIN Sales.SalesOrderHeader soh ON sod.SalesOrderID = soh.SalesOrderID
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                GROUP BY c.CustomerID
                HAVING COUNT(sod.ProductID) > 10
                ORDER BY ProductCount DESC;";
            return conn.Query<CustomerProductCountDto>(sql).ToList();
        }

        [Benchmark]
        public List<CustomerProductCountDto> RepoDb_MSSQL()
        {
            using var conn = CreateMssqlConnection();
            var sql = @"
                SELECT c.CustomerID, COUNT(sod.ProductID) AS ProductCount
                FROM Sales.SalesOrderDetail sod
                JOIN Sales.SalesOrderHeader soh ON sod.SalesOrderID = soh.SalesOrderID
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                GROUP BY c.CustomerID
                HAVING COUNT(sod.ProductID) > 10
                ORDER BY ProductCount DESC;";
            return conn.ExecuteQuery<CustomerProductCountDto>(sql).ToList();
        }

        [Benchmark]
        public List<CustomerProductCountDto> OrmLite_MSSQL_RawSql()
        {
            using var db = CreateOrmLiteMssqlConnection();
            var sql = @"
                SELECT c.CustomerID, COUNT(sod.ProductID) AS ProductCount
                FROM Sales.SalesOrderDetail sod
                JOIN Sales.SalesOrderHeader soh ON sod.SalesOrderID = soh.SalesOrderID
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                GROUP BY c.CustomerID
                HAVING COUNT(sod.ProductID) > 10
                ORDER BY ProductCount DESC";
            return db.SqlList<CustomerProductCountDto>(sql);
        }

        [Benchmark]
        public List<CustomerProductCountDto> OrmLite_MSSQL_LinqStyle()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var q = db.From<SalesOrderDetail>()
                .Join<SalesOrderDetail, SalesOrderHeader>((sod, soh) => sod.SalesOrderId == soh.SalesOrderId)
                .Join<SalesOrderHeader, Customer>((soh, c) => soh.CustomerId == c.CustomerId)
                .GroupBy<SalesOrderDetail, SalesOrderHeader, Customer>((sod, soh, c) => c.CustomerId)
                .Having("COUNT(salesorderdetail.productid) > 10")
                .Select("customer.customerid, COUNT(salesorderdetail.productid) AS ProductCount")
                .OrderByDescending("ProductCount");

            return db.Select<CustomerProductCountDto>(q);
        }
    }
    public class CustomerProductCountDto
    {
        public int CustomerId { get; set; }
        public int ProductCount { get; set; }
    }

}
