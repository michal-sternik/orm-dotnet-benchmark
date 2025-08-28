using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using RepoDb;
using ServiceStack.OrmLite;
using SqlSugar;
using System.Collections.Generic;
using System.Linq;

namespace OrmBenchmarkMag.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class ProductsOrderedMostOftenSortedBenchmarkMssql : OrmBenchmarkBase
    {
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private IFreeSql _freeSqlMssql;

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

    

        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(SqlSugar_ORM))]
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
        public List<ProductOrderSummaryDto> Dapper_ORM()
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
        public List<ProductOrderSummaryDto> RepoDb_ORM()
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
        public List<ProductOrderSummaryDto> SqlSugar_ORM()
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
                  pc.Name AS Category, psc.Name AS Subcategory, p.Name AS ProductName,
                  SUM(sod.OrderQty) AS TotalQty
                FROM Sales.SalesOrderDetail sod
                JOIN Production.Product p ON sod.ProductID = p.ProductID
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                GROUP BY pc.Name, psc.Name, p.Name
                ORDER BY TotalQty DESC";
            return _sqlSugarClient.Ado.SqlQuery<ProductOrderSummaryDto>(sql);
        }
        [Benchmark]
        public List<ProductOrderSummaryDto> OrmLite_ORM()
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
        public List<ProductOrderSummaryDto> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
             .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
             .UseAutoSyncStructure(false)
             .Build();
            return _freeSqlMssql.Ado.Query<ProductOrderSummaryDto>(@"
                SELECT 
                  pc.Name AS Category, psc.Name AS Subcategory, p.Name AS ProductName,
                  SUM(sod.OrderQty) AS TotalQty
                FROM Sales.SalesOrderDetail sod
                JOIN Production.Product p ON sod.ProductID = p.ProductID
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                GROUP BY pc.Name, psc.Name, p.Name
                ORDER BY TotalQty DESC
            ").ToList();
        }
        [Benchmark]
        public List<ProductOrderSummaryDto> EFCore_ORM()
        {
            using var context = CreateMssqlContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return (from sod in context.SalesOrderDetails
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
