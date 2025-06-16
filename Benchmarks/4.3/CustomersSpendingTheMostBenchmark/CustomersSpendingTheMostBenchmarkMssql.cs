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
        public List<CustomerSpendingDto> FreeSql_MSSQL()
        {
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
        public List<CustomerSpendingDto> SqlSugar_MSSQL()
        {
            var list = _sqlSugarClient.Queryable<SalesOrderHeader>()
                .LeftJoin<Customer>((soh, c) => soh.CustomerId == c.CustomerId)
                .LeftJoin<Address>((soh, c, a) => soh.BillToAddressId == a.AddressId)
                .GroupBy((soh, c, a) => new { c.CustomerId, a.City })
                .OrderBy((soh, c, a) => SqlFunc.AggregateSum(soh.TotalDue), OrderByType.Desc)
                .Select((soh, c, a) => new CustomerSpendingDto
                {
                    CustomerId = c.CustomerId,
                    Region = a.City,
                    TotalSpent = SqlFunc.AggregateSum(soh.TotalDue)
                })
                .ToList();

            return list;
        }


        [Benchmark]
        public List<CustomerSpendingDto> EFCore_MSSQL_WithJoins()
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

        [Benchmark]
        public List<CustomerSpendingDto> EFCore_MSSQL_WithIncludeAndGroup()
        {
            using var context = CreateMssqlContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return context.SalesOrderHeaders
                .Include(soh => soh.Customer)
                .Include(soh => soh.BillToAddress)
                .GroupBy(soh => new
                {
                    CustomerId = soh.Customer.CustomerId,
                    Region = soh.BillToAddress.City
                })
                .Select(g => new CustomerSpendingDto
                {
                    CustomerId = g.Key.CustomerId,
                    Region = g.Key.Region,
                    TotalSpent = g.Sum(x => x.TotalDue)
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToList();
        }

        [Benchmark]
        public List<CustomerSpendingDto> Dapper_MSSQL()
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
        public List<CustomerSpendingDto> RepoDb_MSSQL()
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
        public List<CustomerSpendingDto> OrmLite_MSSQL_RawSql()
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
        public List<CustomerSpendingDto> OrmLite_MSSQL_LinqStyle()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var q = db.From<SalesOrderHeader>()
                .Join<SalesOrderHeader, Customer>((soh, c) => soh.CustomerId == c.CustomerId)
                .Join<SalesOrderHeader, Address>((soh, a) => soh.BillToAddressId == a.AddressId)
                .GroupBy<SalesOrderHeader, Customer, Address>((soh, c, a) => new { c.CustomerId, a.City })
                .Select<SalesOrderHeader, Customer, Address>((soh, c, a) => new
                {
                    CustomerId = c.CustomerId,
                    Region = a.City,
                    TotalSpent = Sql.Sum(soh.TotalDue)
                })
                .OrderByDescending("TotalSpent");

            return db.Select<CustomerSpendingDto>(q);
        }
    }
    public class CustomerSpendingDto
    {
        public int CustomerId { get; set; }
        public string Region { get; set; }
        public decimal TotalSpent { get; set; }
    }

}
