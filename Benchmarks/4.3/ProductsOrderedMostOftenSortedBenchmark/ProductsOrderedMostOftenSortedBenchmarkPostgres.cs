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
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDb() => PostgresRepoDbMappingSetup.Init();

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
        public List<ProductOrderSummaryDto> FreeSql_Postgres()
        {
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

        [GlobalSetup(Target = nameof(SqlSugar_Postgres))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
        }
        [Benchmark]
        public List<ProductOrderSummaryDto> SqlSugar_Postgres()
        {
            var list = _sqlSugarClient.Queryable<SalesOrderDetail>()
                .LeftJoin<Product>((sod, p) => sod.ProductId == p.ProductId)
                .LeftJoin<ProductSubcategory>((sod, p, psc) => p.ProductSubcategoryId == psc.ProductSubcategoryId)
                .LeftJoin<ProductCategory>((sod, p, psc, pc) => psc.ProductCategoryId == pc.ProductCategoryId)
                .GroupBy((sod, p, psc, pc) => new { category = pc.Name, subcategory = psc.Name, productname = p.Name })
                .OrderBy("SUM(sod.orderqty) DESC")
                .Select<dynamic>("pc.name AS category, psc.name AS subcategory, p.name AS productname, SUM(sod.orderqty) AS totalqty")
                .ToList();

            return list.Select(x => new ProductOrderSummaryDto
            {
                Category = x.category,
                Subcategory = x.subcategory,
                ProductName = x.productname,
                TotalQty = Convert.ToInt32(x.totalqty)
            }).ToList();
        }


        [Benchmark]
        public List<ProductOrderSummaryDto> EFCore_Postgres()
        {
            using var context = CreatePostgresContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return context.SalesOrderDetails
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
                    TotalQty = g.Sum(x => x.OrderQty)
                })
                .OrderByDescending(x => x.TotalQty)
                .ToList();
        }

        [Benchmark]
        public List<ProductOrderSummaryDto> Dapper_Postgres()
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
        public List<ProductOrderSummaryDto> RepoDb_Postgres()
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
        public List<ProductOrderSummaryDto> OrmLite_Postgres_LinqStyle()
        {
            using var db = CreateOrmLitePostgresConnection();

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
}
