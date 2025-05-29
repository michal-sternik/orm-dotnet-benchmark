using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Data;
using OrmBenchmarkMag.Models;
using RepoDb;
using ServiceStack.OrmLite;
using System.Collections.Generic;
using System.Linq;

namespace OrmBenchmarkMag.Benchmarks
{

    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class ProductsOrderedMostOftenSortedBenchmarkMssql : OrmBenchmarkBase
    {
        [Benchmark]
        public List<ProductOrderSummaryDto> EFCore_MSSQL_WithJoins()
        {
            using var context = CreateMssqlContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var result = (from sod in context.SalesOrderDetails
                          join p in context.Products on sod.ProductId equals p.ProductId
                          join psc in context.ProductSubcategories on p.ProductSubcategoryId equals psc.ProductSubcategoryId
                          join pc in context.ProductCategories on psc.ProductCategoryId equals pc.ProductCategoryId
                          group sod by new { CategoryName = pc.Name, SubcategoryName = psc.Name, ProductName = p.Name } into g
                          orderby g.Sum(x => x.OrderQty) descending
                          select new ProductOrderSummaryDto
                          {
                              Category = g.Key.CategoryName,
                              Subcategory = g.Key.SubcategoryName,
                              ProductName = g.Key.ProductName,
                              TotalQty = g.Sum(x => x.OrderQty)
                          }).ToList();

            return result;
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> EFCore_MSSQL_WithIncludeAndGroup()
        {
            using var context = CreateMssqlContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var result = context.SalesOrderDetails
                .Include(sod => sod.Product)
                    .ThenInclude(p => p.ProductSubcategory)
                        .ThenInclude(psc => psc.ProductCategory)
                .GroupBy(sod => new
                {
                    Category = sod.Product.ProductSubcategory.ProductCategory.Name,
                    Subcategory = sod.Product.ProductSubcategory.Name,
                    ProductName = sod.Product.Name
                })
                .Select(g => new ProductOrderSummaryDto
                {
                    Category = g.Key.Category,
                    Subcategory = g.Key.Subcategory,
                    ProductName = g.Key.ProductName,
                    TotalQty = g.Sum(sod => sod.OrderQty)
                })
                .OrderByDescending(r => r.TotalQty)
                .ToList();

            return result;
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> Dapper_MSSQL()
        {
            using var connection = CreateMssqlConnection();

            var sql = @"
                SELECT 
                  pc.Name AS Category, psc.Name AS Subcategory, p.Name AS ProductName,
                  SUM(sod.OrderQty) AS TotalQty
                FROM Sales.SalesOrderDetail sod
                JOIN Production.Product p ON sod.ProductID = p.ProductID
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                GROUP BY pc.Name, psc.Name, p.Name
                ORDER BY TotalQty DESC;";

            return connection.Query<ProductOrderSummaryDto>(sql).ToList();
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> RepoDb_MSSQL()
        {
            using var connection = CreateMssqlConnection();

            var sql = @"
                SELECT 
                  pc.Name AS Category, psc.Name AS Subcategory, p.Name AS ProductName,
                  SUM(sod.OrderQty) AS TotalQty
                FROM Sales.SalesOrderDetail sod
                JOIN Production.Product p ON sod.ProductID = p.ProductID
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                GROUP BY pc.Name, psc.Name, p.Name
                ORDER BY TotalQty DESC;";

            return connection.ExecuteQuery<ProductOrderSummaryDto>(sql).ToList();
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> OrmLite_MSSQL_RawSql()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var sql = @"
                SELECT 
                  pc.Name AS Category, psc.Name AS Subcategory, p.Name AS ProductName,
                  SUM(sod.OrderQty) AS TotalQty
                FROM Sales.SalesOrderDetail sod
                JOIN Production.Product p ON sod.ProductID = p.ProductID
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                GROUP BY pc.Name, psc.Name, p.Name
                ORDER BY TotalQty DESC";

            return db.SqlList<ProductOrderSummaryDto>(sql);
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> OrmLite_MSSQL_LinqStyle()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var q = db.From<SalesOrderDetail>()
                .Join<SalesOrderDetail, Product>((sod, p) => sod.ProductId == p.ProductId)
                .Join<Product, ProductSubcategory>((p, psc) => p.ProductSubcategoryId == psc.ProductSubcategoryId)
                .Join<ProductSubcategory, ProductCategory>((psc, pc) => psc.ProductCategoryId == pc.ProductCategoryId)
                .GroupBy<SalesOrderDetail, Product, ProductSubcategory, ProductCategory>((sod, p, psc, pc) => new
                {
                    CategoryName = pc.Name,
                    SubcategoryName = psc.Name,
                    ProductName = p.Name
                })
                .Select<SalesOrderDetail, Product, ProductSubcategory, ProductCategory>((sod, p, psc, pc) => new
                {
                    Category = pc.Name,
                    Subcategory = psc.Name,
                    ProductName = p.Name,
                    TotalQty = Sql.Sum(sod.OrderQty)
                })
                .OrderByDescending("TotalQty");

            return db.Select<ProductOrderSummaryDto>(q);
        }
    }
    public class ProductOrderSummaryDto
    {
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string ProductName { get; set; }
        public int TotalQty { get; set; }
    }
}
