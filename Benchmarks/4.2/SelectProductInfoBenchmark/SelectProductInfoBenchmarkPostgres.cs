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
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDb()
        {
            RepoDbSchemaConfigurator.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_ORM))]
        public void SetupOrmLite()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }

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
        public List<ProductInfoDto> Dapper_ORM()
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
        public List<ProductInfoDto> RepoDb_ORM()
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
        public List<ProductInfoDto> SqlSugar_ORM()
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
                    p.name AS ProductName,
                    pc.name AS Category,
                    psc.name AS Subcategory,
                    CONCAT(COALESCE(wu.name, ''), '/', COALESCE(su.name, '')) AS Units
                FROM production.product p
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                LEFT JOIN production.unitmeasure wu ON p.weightunitmeasurecode = wu.unitmeasurecode
                LEFT JOIN production.unitmeasure su ON p.sizeunitmeasurecode = su.unitmeasurecode";
            return _sqlSugarClient.Ado.SqlQuery<ProductInfoDto>(sql);
        }
        [Benchmark]
        public List<ProductInfoDto> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();

            var sql = @"
                SELECT 
                    p.name AS ProductName,
                    pc.name AS Category,
                    psc.name AS Subcategory,
                    CONCAT(COALESCE(wu.name, ''), '/', COALESCE(su.name, '')) AS Units
                FROM production.product p
                JOIN production.productsubcategory psc ON p.productsubcategoryid = psc.productsubcategoryid
                JOIN production.productcategory pc ON psc.productcategoryid = pc.productcategoryid
                LEFT JOIN production.unitmeasure wu ON p.weightunitmeasurecode = wu.unitmeasurecode
                LEFT JOIN production.unitmeasure su ON p.sizeunitmeasurecode = su.unitmeasurecode";

            return db.SqlList<ProductInfoDto>(sql);
        }
        [Benchmark]
        public List<ProductInfoDto> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
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
        [Benchmark]
        public List<ProductInfoDto> EFCore_ORM()
        {
            using var context = CreatePostgresContext();

            return (from p in context.Products
                    join sub in context.ProductSubcategories on p.ProductSubcategoryId equals sub.ProductSubcategoryId
                    join cat in context.ProductCategories on sub.ProductCategoryId equals cat.ProductCategoryId
                    join wu in context.UnitMeasures on p.WeightUnitMeasureCode equals wu.UnitMeasureCode into wuJoin
                    from wu in wuJoin.DefaultIfEmpty()
                    join su in context.UnitMeasures on p.SizeUnitMeasureCode equals su.UnitMeasureCode into suJoin
                    from su in suJoin.DefaultIfEmpty()
                    select new ProductInfoDto
                    {
                        ProductName = p.Name,
                        Category = cat.Name,
                        Subcategory = sub.Name,
                        Units = (wu.Name ?? "") + "/" + (su.Name ?? "")
                    }).ToList();
        }
    }
}
