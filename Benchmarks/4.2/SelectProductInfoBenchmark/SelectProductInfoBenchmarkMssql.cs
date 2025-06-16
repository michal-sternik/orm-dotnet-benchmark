using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Benchmarks;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using RepoDb;
using ServiceStack.OrmLite;
using SqlSugar;
using System.Collections.Generic;
using System.Linq;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectProductInfoBenchmarkMssql : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_MSSQL))]
        public void SetupRepoDbMssql()
        {
            FluentMapper.Entity<Product>().Table("Production.Product");
            FluentMapper.Entity<ProductSubcategory>().Table("Production.ProductSubcategory");
            FluentMapper.Entity<ProductCategory>().Table("Production.ProductCategory");
            FluentMapper.Entity<UnitMeasure>().Table("Production.UnitMeasure");
        }

        private IFreeSql _freeSqlMssql;

        [GlobalSetup(Target = nameof(FreeSql_MSSQL))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        [Benchmark]
        public List<ProductInfoDto> FreeSql_MSSQL()
        {
            return _freeSqlMssql.Ado.Query<ProductInfoDto>(@"
                SELECT 
                    p.Name AS ProductName,
                    pc.Name AS Category,
                    psc.Name AS Subcategory,
                    CONCAT(wu.Name, '/', su.Name) AS Units
                FROM Production.Product p
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                LEFT JOIN Production.UnitMeasure wu ON p.WeightUnitMeasureCode = wu.UnitMeasureCode
                LEFT JOIN Production.UnitMeasure su ON p.SizeUnitMeasureCode = su.UnitMeasureCode
            ").ToList();
        }


        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(SqlSugar_MSSQL))]
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
        public List<ProductInfoDto> SqlSugar_MSSQL()
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
        public List<ProductInfoDto> Dapper_MSSQL()
        {
            using var conn = CreateMssqlConnection();

            return conn.Query<ProductInfoDto>(@"
                SELECT 
                    p.Name AS ProductName,
                    pc.Name AS Category,
                    psc.Name AS Subcategory,
                    CONCAT(wu.Name, '/', su.Name) AS Units
                FROM Production.Product p
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                LEFT JOIN Production.UnitMeasure wu ON p.WeightUnitMeasureCode = wu.UnitMeasureCode
                LEFT JOIN Production.UnitMeasure su ON p.SizeUnitMeasureCode = su.UnitMeasureCode
            ").ToList();
        }

        [Benchmark]
        public List<ProductInfoDto> RepoDb_MSSQL()
        {
            using var conn = CreateMssqlConnection();

            return (List<ProductInfoDto>)conn.ExecuteQuery<ProductInfoDto>(@"
                SELECT 
                    p.Name AS ProductName,
                    pc.Name AS Category,
                    psc.Name AS Subcategory,
                    CONCAT(wu.Name, '/', su.Name) AS Units
                FROM Production.Product p
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                LEFT JOIN Production.UnitMeasure wu ON p.WeightUnitMeasureCode = wu.UnitMeasureCode
                LEFT JOIN Production.UnitMeasure su ON p.SizeUnitMeasureCode = su.UnitMeasureCode
            ");
        }

        [Benchmark]
        public List<ProductInfoDto> OrmLite_MSSQL()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var sql = @"
                SELECT 
                    p.Name AS ProductName,
                    pc.Name AS Category,
                    psc.Name AS Subcategory,
                    CONCAT(wu.Name, '/', su.Name) AS Units
                FROM Production.Product p
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                LEFT JOIN Production.UnitMeasure wu ON p.WeightUnitMeasureCode = wu.UnitMeasureCode
                LEFT JOIN Production.UnitMeasure su ON p.SizeUnitMeasureCode = su.UnitMeasureCode";

            return db.SqlList<ProductInfoDto>(sql);
        }

        [Benchmark]
        public List<ProductInfoDto> OrmLite_MSSQL_LinqStyle()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var q = db.From<Product>()
                .Join<Product, ProductSubcategory>((p, psc) => p.ProductSubcategoryId == psc.ProductSubcategoryId)
                .Join<ProductSubcategory, ProductCategory>((psc, pc) => psc.ProductCategoryId == pc.ProductCategoryId)
                .LeftJoin<Product, UnitMeasure>((p, wu) => p.WeightUnitMeasureCode == wu.UnitMeasureCode, db.TableAlias("wu"))
                .LeftJoin<Product, UnitMeasure>((p, su) => p.SizeUnitMeasureCode == su.UnitMeasureCode, db.TableAlias("su"))
                .Select("Production.Product.Name, Production.ProductCategory.Name, Production.ProductSubcategory.Name, wu.Name + '/' + su.Name");
        

            return db.Select<ProductInfoDto>(q);
        }

        [Benchmark]
        public List<ProductInfoDto> EFCore_MSSQL()
        {
            using var context = CreateMssqlContext();

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

        [Benchmark]
        public List<ProductInfoDto> EFCore_MSSQL_IncludeStyle()
        {
            using var context = CreateMssqlContext();

            return context.Products
                .Include(p => p.ProductSubcategory)
                    .ThenInclude(psc => psc.ProductCategory)
                .Include(p => p.WeightUnitMeasureCodeNavigation)
                .Include(p => p.SizeUnitMeasureCodeNavigation)
                .Select(p => new ProductInfoDto
                {
                    ProductName = p.Name,
                    Category = p.ProductSubcategory.ProductCategory.Name,
                    Subcategory = p.ProductSubcategory.Name,
                    Units = (p.WeightUnitMeasureCode ?? "") + "/" + (p.SizeUnitMeasureCode ?? "")
                })
                .ToList();
        }


    }
}
public class ProductInfoDto
{
    public string ProductName { get; set; }
    public string Category { get; set; }
    public string Subcategory { get; set; }
    public string Units { get; set; }
}