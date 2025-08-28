using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using ServiceStack.OrmLite;
using OrmBenchmarkMag.Benchmarks;
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectOrderAndCustomerInformationsBenchmarkMssql : OrmBenchmarkBase
    {
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        [GlobalSetup(Target = nameof(RepoDb))]
        public void SetupRepoDbMssql()
        {
            FluentMapper.Entity<SalesOrderHeader>().Table("Sales.SalesOrderHeader");
            FluentMapper.Entity<SalesOrderDetail>().Table("Sales.SalesOrderDetail");
            FluentMapper.Entity<Product>().Table("Production.Product");
            FluentMapper.Entity<Customer>().Table("Sales.Customer");
            FluentMapper.Entity<Person>().Table("Person.Person");
            FluentMapper.Entity<Address>().Table("Person.Address");
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
        public List<OrderProductDetailDto> Dapper_ORM()
        {
            using var conn = CreateMssqlConnection();
            return conn.Query<OrderProductDetailDto>(
                @"SELECT soh.SalesOrderID, p.Name AS ProductName, sod.OrderQty, pe.FirstName, pe.LastName, c.AccountNumber, a.City
                  FROM Sales.SalesOrderHeader soh
                  JOIN Sales.SalesOrderDetail sod ON soh.SalesOrderID = sod.SalesOrderID
                  JOIN Production.Product p ON sod.ProductID = p.ProductID
                  JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                  LEFT JOIN Person.Person pe ON c.CustomerID = pe.BusinessEntityID
                  JOIN Person.Address a ON soh.ShipToAddressID = a.AddressID
                  ORDER BY soh.SalesOrderID")
                .ToList();
        }

        [Benchmark]
        public List<OrderProductDetailDto> RepoDb_ORM()
        {
            using var conn = CreateMssqlConnection();
            return conn.ExecuteQuery<OrderProductDetailDto>(
                @"SELECT soh.SalesOrderID, p.Name AS ProductName, sod.OrderQty, pe.FirstName, pe.LastName, c.AccountNumber, a.City
                  FROM Sales.SalesOrderHeader soh
                  JOIN Sales.SalesOrderDetail sod ON soh.SalesOrderID = sod.SalesOrderID
                  JOIN Production.Product p ON sod.ProductID = p.ProductID
                  JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                  LEFT JOIN Person.Person pe ON c.CustomerID = pe.BusinessEntityID
                  JOIN Person.Address a ON soh.ShipToAddressID = a.AddressID
                  ORDER BY soh.SalesOrderID")
                .ToList();
        }
        [Benchmark]
        public List<OrderProductDetailDto> SqlSugar_ORM()
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
                SELECT soh.SalesOrderID, p.Name AS ProductName, sod.OrderQty, pe.FirstName, pe.LastName, c.AccountNumber, a.City
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.SalesOrderDetail sod ON soh.SalesOrderID = sod.SalesOrderID
                JOIN Production.Product p ON sod.ProductID = p.ProductID
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                LEFT JOIN Person.Person pe ON c.CustomerID = pe.BusinessEntityID
                JOIN Person.Address a ON soh.ShipToAddressID = a.AddressID
                ORDER BY soh.SalesOrderID";
            return _sqlSugarClient.Ado.SqlQuery<OrderProductDetailDto>(sql);
        }


        [Benchmark]
        public List<OrderProductDetailDto> OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();
            var sql = @"
                SELECT soh.SalesOrderID, p.Name AS ProductName, sod.OrderQty, pe.FirstName, pe.LastName, c.AccountNumber, a.City
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.SalesOrderDetail sod ON soh.SalesOrderID = sod.SalesOrderID
                JOIN Production.Product p ON sod.ProductID = p.ProductID
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                LEFT JOIN Person.Person pe ON c.CustomerID = pe.BusinessEntityID
                JOIN Person.Address a ON soh.ShipToAddressID = a.AddressID
                ORDER BY soh.SalesOrderID";

            return db.SqlList<OrderProductDetailDto>(sql);
        }
        [Benchmark]
        public List<OrderProductDetailDto> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
             .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
             .UseAutoSyncStructure(false)
             .Build();
            return _freeSqlMssql.Ado.Query<OrderProductDetailDto>(@"
                SELECT soh.SalesOrderID, p.Name AS ProductName, sod.OrderQty, pe.FirstName, pe.LastName, c.AccountNumber, a.City
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.SalesOrderDetail sod ON soh.SalesOrderID = sod.SalesOrderID
                JOIN Production.Product p ON sod.ProductID = p.ProductID
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                LEFT JOIN Person.Person pe ON c.CustomerID = pe.BusinessEntityID
                JOIN Person.Address a ON soh.ShipToAddressID = a.AddressID
                ORDER BY soh.SalesOrderID
            ").ToList();
        }
        [Benchmark]
        public List<OrderProductDetailDto> EFCore_ORM()
        {
            using var context = CreateMssqlContext();
            return (from soh in context.SalesOrderHeaders
                    join sod in context.SalesOrderDetails on soh.SalesOrderId equals sod.SalesOrderId
                    join p in context.Products on sod.ProductId equals p.ProductId
                    join c in context.Customers on soh.CustomerId equals c.CustomerId
                    join a in context.Addresses on soh.ShipToAddressId equals a.AddressId
                    join pe in context.People on c.CustomerId equals pe.BusinessEntityId into peJoin
                    from pe in peJoin.DefaultIfEmpty()
                    orderby soh.SalesOrderId
                    select new OrderProductDetailDto
                    {
                        SalesOrderId = soh.SalesOrderId,
                        ProductName = p.Name,
                        OrderQty = sod.OrderQty,
                        FirstName = pe.FirstName,
                        LastName = pe.LastName,
                        AccountNumber = c.AccountNumber,
                        City = a.City
                    }).ToList();
        }
    }

    public class OrderProductDetailDto
    {
        public int SalesOrderId { get; set; }
        public string ProductName { get; set; }
        public short OrderQty { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AccountNumber { get; set; }
        public string City { get; set; }
    }
}
