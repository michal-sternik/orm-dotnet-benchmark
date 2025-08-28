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
    public class ProductsOrderedMostOftenSortedBenchmarkPostgres : OrmBenchmarkBase
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

        [Benchmark]
        public List<ProductOrderSummaryDto> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<ProductOrderSummaryDto>(@"
                SELECT 
                  pc.name AS Category,
                  psc.name AS Subcategory,
                  p.name AS ProductName,
                  SUM(sod.orderqty) AS TotalQty
                FROM sales.salesorderdetail sod
                JOIN production.product p ON sod.productid = p.productid
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                GROUP BY pc.name, psc.name, p.name
                ORDER BY TotalQty DESC
            ").ToList();
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
        public List<ProductOrderSummaryDto> SqlSugar_ORM()
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
                  pc.name AS Category,
                  psc.name AS Subcategory,
                  p.name AS ProductName,
                  SUM(sod.orderqty) AS TotalQty
                FROM sales.salesorderdetail sod
                JOIN production.product p ON sod.productid = p.productid
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                GROUP BY pc.name, psc.name, p.name
                ORDER BY TotalQty DESC";
            return _sqlSugarClient.Ado.SqlQuery<ProductOrderSummaryDto>(sql);
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> EFCore_ORM()
        {
            using var context = CreatePostgresContext();
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

        [Benchmark]
        public List<ProductOrderSummaryDto> Dapper_ORM()
        {
            using var conn = CreatePostgresConnection();
            var sql = @"
                SELECT 
                  pc.name AS Category,
                  psc.name AS Subcategory,
                  p.name AS ProductName,
                  SUM(sod.orderqty) AS TotalQty
                FROM sales.salesorderdetail sod
                JOIN production.product p ON sod.productid = p.productid
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                GROUP BY pc.name, psc.name, p.name
                ORDER BY TotalQty DESC";
            return conn.Query<ProductOrderSummaryDto>(sql).ToList();
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> RepoDb_ORM()
        {
            using var conn = CreatePostgresConnection();
            var sql = @"
                SELECT 
                  pc.name AS Category,
                  psc.name AS Subcategory,
                  p.name AS ProductName,
                  SUM(sod.orderqty) AS TotalQty
                FROM sales.salesorderdetail sod
                JOIN production.product p ON sod.productid = p.productid
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                GROUP BY pc.name, psc.name, p.name
                ORDER BY TotalQty DESC";
            return conn.ExecuteQuery<ProductOrderSummaryDto>(sql).ToList();
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();

            var sql = @"
                SELECT 
                  pc.name AS Category,
                  psc.name AS Subcategory,
                  p.name AS ProductName,
                  SUM(sod.orderqty) AS TotalQty
                FROM sales.salesorderdetail sod
                JOIN production.product p ON sod.productid = p.productid
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                GROUP BY pc.name, psc.name, p.name
                ORDER BY TotalQty DESC";

            return db.SqlList<ProductOrderSummaryDto>(sql);
        }
    }
}
