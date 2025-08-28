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
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDbMssql()
        {
            FluentMapper.Entity<Product>().Table("Production.Product");
            FluentMapper.Entity<ProductSubcategory>().Table("Production.ProductSubcategory");
            FluentMapper.Entity<ProductCategory>().Table("Production.ProductCategory");
            FluentMapper.Entity<UnitMeasure>().Table("Production.UnitMeasure");
        }

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
        public List<ProductInfoDto> Dapper_ORM()
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
        public List<ProductInfoDto> RepoDb_ORM()
        {
            using var conn = CreateMssqlConnection();
            return conn.ExecuteQuery<ProductInfoDto>(@"
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
        public List<ProductInfoDto> SqlSugar_ORM()
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
                    p.Name AS ProductName,
                    pc.Name AS Category,
                    psc.Name AS Subcategory,
                    CONCAT(wu.Name, '/', su.Name) AS Units
                FROM Production.Product p
                JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                LEFT JOIN Production.UnitMeasure wu ON p.WeightUnitMeasureCode = wu.UnitMeasureCode
                LEFT JOIN Production.UnitMeasure su ON p.SizeUnitMeasureCode = su.UnitMeasureCode";
            return _sqlSugarClient.Ado.SqlQuery<ProductInfoDto>(sql);
        }
        [Benchmark]
        public List<ProductInfoDto> OrmLite_ORM()
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
        public List<ProductInfoDto> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
             .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
             .UseAutoSyncStructure(false)
             .Build();
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
        [Benchmark]
        public List<ProductInfoDto> EFCore_ORM()
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
    }
}

public class ProductInfoDto
{
    public string ProductName { get; set; }
    public string Category { get; set; }
    public string Subcategory { get; set; }
    public string Units { get; set; }
}
