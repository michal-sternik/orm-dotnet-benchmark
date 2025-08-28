using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using ServiceStack.OrmLite;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using SqlSugar;

namespace OrmBenchmarkMag.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class CustomersSpendingTheMostBenchmarkMssql : OrmBenchmarkBase
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
        public List<CustomerSpendingDto> Dapper_ORM()
        {
            using var conn = CreateMssqlConnection();
            var sql = @"
                SELECT 
                    c.CustomerID, 
                    a.City AS Region, 
                    SUM(soh.TotalDue) AS TotalSpent
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                GROUP BY c.CustomerID, a.City
                ORDER BY TotalSpent DESC;";
            return conn.Query<CustomerSpendingDto>(sql).ToList();
        }

        [Benchmark]
        public List<CustomerSpendingDto> RepoDb_ORM()
        {
            using var conn = CreateMssqlConnection();
            var sql = @"
                SELECT 
                    c.CustomerID, 
                    a.City AS Region, 
                    SUM(soh.TotalDue) AS TotalSpent
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                GROUP BY c.CustomerID, a.City
                ORDER BY TotalSpent DESC;";
            return conn.ExecuteQuery<CustomerSpendingDto>(sql).ToList();
        }
        [Benchmark]
        public List<CustomerSpendingDto> SqlSugar_ORM()
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
                    c.CustomerID, 
                    a.City AS Region, 
                    SUM(soh.TotalDue) AS TotalSpent
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                GROUP BY c.CustomerID, a.City
                ORDER BY TotalSpent DESC";
            return _sqlSugarClient.Ado.SqlQuery<CustomerSpendingDto>(sql);
        }
        [Benchmark]
        public List<CustomerSpendingDto> OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();
            var sql = @"
                SELECT 
                    c.CustomerID, 
                    a.City AS Region, 
                    SUM(soh.TotalDue) AS TotalSpent
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                GROUP BY c.CustomerID, a.City
                ORDER BY TotalSpent DESC";
            return db.SqlList<CustomerSpendingDto>(sql);
        }
        [Benchmark]
        public List<CustomerSpendingDto> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
             .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
             .UseAutoSyncStructure(false)
             .Build();
            return _freeSqlMssql.Ado.Query<CustomerSpendingDto>(@"
                SELECT 
                    c.CustomerID, 
                    a.City AS Region, 
                    SUM(soh.TotalDue) AS TotalSpent
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                JOIN Person.Address a ON soh.BillToAddressID = a.AddressID
                GROUP BY c.CustomerID, a.City
                ORDER BY TotalSpent DESC
            ").ToList();
        }
        [Benchmark]
        public List<CustomerSpendingDto> EFCore_ORM()
        {
            using var context = CreateMssqlContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return (from soh in context.SalesOrderHeaders
                    join c in context.Customers on soh.CustomerId equals c.CustomerId
                    join a in context.Addresses on soh.BillToAddressId equals a.AddressId
                    group soh by new { c.CustomerId, a.City } into g
                    orderby g.Sum(x => x.TotalDue) descending
                    select new CustomerSpendingDto
                    {
                        CustomerId = g.Key.CustomerId,
                        Region = g.Key.City,
                        TotalSpent = g.Sum(x => x.TotalDue)
                    }).ToList();
        }
    }

    public class CustomerSpendingDto
    {
        public int CustomerId { get; set; }
        public string Region { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
