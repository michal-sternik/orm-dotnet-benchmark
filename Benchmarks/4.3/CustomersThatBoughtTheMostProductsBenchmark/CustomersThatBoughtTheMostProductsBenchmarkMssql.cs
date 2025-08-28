using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using ServiceStack.OrmLite;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using SqlSugar;
using ServiceStack;

namespace OrmBenchmarkMag.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class CustomersThatBoughtTheMostProductsBenchmarkMssql : OrmBenchmarkBase
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
        public List<CustomerProductCountDto> Dapper_ORM()
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
        public List<CustomerProductCountDto> RepoDb_ORM()
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
        public List<CustomerProductCountDto> SqlSugar_ORM()
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
                SELECT c.CustomerID, COUNT(sod.ProductID) AS ProductCount
                FROM Sales.SalesOrderDetail sod
                JOIN Sales.SalesOrderHeader soh ON sod.SalesOrderID = soh.SalesOrderID
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                GROUP BY c.CustomerID
                HAVING COUNT(sod.ProductID) > 10
                ORDER BY ProductCount DESC";
            return _sqlSugarClient.Ado.SqlQuery<CustomerProductCountDto>(sql);
        }
        [Benchmark]
        public List<CustomerProductCountDto> OrmLite_ORM()
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
        public List<CustomerProductCountDto> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
             .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
             .UseAutoSyncStructure(false)
             .Build();
            return _freeSqlMssql.Ado.Query<CustomerProductCountDto>(@"
                SELECT c.CustomerID, COUNT(sod.ProductID) AS ProductCount
                FROM Sales.SalesOrderDetail sod
                JOIN Sales.SalesOrderHeader soh ON sod.SalesOrderID = soh.SalesOrderID
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                GROUP BY c.CustomerID
                HAVING COUNT(sod.ProductID) > 10
                ORDER BY ProductCount DESC
            ").ToList();
        }

        [Benchmark]
        public List<CustomerProductCountDto> EFCore_ORM()
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
    }
    public class CustomerProductCountDto
    {
        public int CustomerId { get; set; }
        public int ProductCount { get; set; }
    }

}
