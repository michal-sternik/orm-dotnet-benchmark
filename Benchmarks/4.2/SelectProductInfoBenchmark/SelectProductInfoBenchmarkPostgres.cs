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
    public class SelectProductInfoBenchmarkPostgresql : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_Postgres))] 
        public void SetupRepoDb()
        {
            RepoDbSchemaConfigurator.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_Postgres_LinqStyle))]
        public void SetupOrmLite()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings(); 
        }

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
        public List<ProductInfoDto> FreeSql_Postgres()
        {
            return _freeSqlPostgres.Ado.Query<ProductInfoDto>(@"
                SELECT 
                    p.name AS ProductName,
                    pc.name AS Category,
                    psc.name AS Subcategory,
                    CONCAT(COALESCE(wu.name, ''), '/', COALESCE(su.name, '')) AS Units
                FROM production.product p
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                LEFT JOIN production.unitmeasure wu ON p.weightunitmeasurecode = wu.unitmeasurecode
                LEFT JOIN production.unitmeasure su ON p.sizeunitmeasurecode = su.unitmeasurecode
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
        public List<ProductInfoDto> SqlSugar_Postgres()
        {
            var list = _sqlSugarClient.Queryable<Product>()
                .LeftJoin<ProductSubcategory>((p, psc) => p.ProductSubcategoryId == psc.ProductSubcategoryId)
                .LeftJoin<ProductCategory>((p, psc, pc) => psc.ProductCategoryId == pc.ProductCategoryId)
                .LeftJoin<UnitMeasure>((p, psc, pc, wu) => p.WeightUnitMeasureCode == wu.UnitMeasureCode)
                .LeftJoin<UnitMeasure>((p, psc, pc, wu, su) => p.SizeUnitMeasureCode == su.UnitMeasureCode)
                .Select((p, psc, pc, wu, su) => new ProductInfoDto
                {
                    ProductName = p.Name,
                    Category = pc.Name,
                    Subcategory = psc.Name,
                    Units = (wu.Name ?? "") + "/" + (su.Name ?? "")
                })
                .ToList();

            return list;
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
