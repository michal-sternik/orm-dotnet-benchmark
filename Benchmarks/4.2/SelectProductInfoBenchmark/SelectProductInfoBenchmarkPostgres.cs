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
    public class SelectProductsWithUnitsBenchmarkPostgres : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDb()
        {
            PostgresRepoDbMappingSetup.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_Postgres_LinqStyle))]
        public void SetupOrmLite()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings(); 
        }

        [Benchmark]
        public List<ProductInfoDto> EFCore_Postgres()
        {
            using var context = CreatePostgresContext();

            return context.Products
                .Include(p => p.ProductSubcategory)
                    .ThenInclude(psc => psc.ProductCategory)
                .Include(p => p.WeightUnitMeasureCodeNavigation)
                .Include(p => p.SizeUnitMeasureCodeNavigation)
                .Select(p => new ProductInfoDto
                {
                    ProductName = p.Name,
                    Category = p.ProductSubcategory != null && p.ProductSubcategory.ProductCategory != null
                        ? p.ProductSubcategory.ProductCategory.Name
                        : string.Empty,
                    Subcategory = p.ProductSubcategory != null
                        ? p.ProductSubcategory.Name
                        : string.Empty,
                    Units = (p.WeightUnitMeasureCodeNavigation != null
                        ? p.WeightUnitMeasureCodeNavigation.Name
                        : "") + "/" + (p.SizeUnitMeasureCodeNavigation != null
                        ? p.SizeUnitMeasureCodeNavigation.Name
                        : "")
                })
                .ToList();
        }

        [Benchmark]
        public List<ProductInfoDto> Dapper_Postgres()
        {
            using var conn = CreatePostgresConnection();
            return conn.Query<ProductInfoDto>(
                @"
                SELECT 
                    p.Name AS ProductName,
                    pc.Name AS Category,
                    psc.Name AS Subcategory,
                    CONCAT(COALESCE(wu.Name, ''), '/', COALESCE(su.Name, '')) AS Units
                FROM production.product p
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                LEFT JOIN production.unitmeasure wu ON p.weightunitmeasurecode = wu.unitmeasurecode
                LEFT JOIN production.unitmeasure su ON p.sizeunitmeasurecode = su.unitmeasurecode"
            ).ToList();
        }

        [Benchmark]
        public List<ProductInfoDto> RepoDb_Postgres()
        {
            using var conn = CreatePostgresConnection();
            return conn.ExecuteQuery<ProductInfoDto>(
                @"
                SELECT 
                    p.Name AS ProductName,
                    pc.Name AS Category,
                    psc.Name AS Subcategory,
                    CONCAT(COALESCE(wu.Name, ''), '/', COALESCE(su.Name, '')) AS Units
                FROM production.product p
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                LEFT JOIN production.unitmeasure wu ON p.weightunitmeasurecode = wu.unitmeasurecode
                LEFT JOIN production.unitmeasure su ON p.sizeunitmeasurecode = su.unitmeasurecode"
            ).ToList();
        }

        [Benchmark]
        public List<ProductInfoDto> OrmLite_Postgres_LinqStyle()
        {
            using var db = CreateOrmLitePostgresConnection();

            var q = db.From<Product>()
                .Join<Product, ProductSubcategory>((p, psc) => p.ProductSubcategoryId == psc.ProductSubcategoryId)
                .Join<ProductSubcategory, ProductCategory>((psc, pc) => psc.ProductCategoryId == pc.ProductCategoryId)
                .LeftJoin<Product, UnitMeasure>((p, wu) => p.WeightUnitMeasureCode == wu.UnitMeasureCode, db.TableAlias("wu"))
                .LeftJoin<Product, UnitMeasure>((p, su) => p.SizeUnitMeasureCode == su.UnitMeasureCode, db.TableAlias("su"))
                .Select(@"
                    production.product.name,
                    production.productcategory.name,
                    production.productsubcategory.name,
                    CONCAT(COALESCE(wu.name, ''), '/', COALESCE(su.name, ''))");

            return db.Select<ProductInfoDto>(q);
        }
    }

}
