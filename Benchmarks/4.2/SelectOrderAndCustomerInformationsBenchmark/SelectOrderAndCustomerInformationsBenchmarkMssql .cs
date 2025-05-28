using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using ServiceStack.OrmLite;
using OrmBenchmarkMag.Benchmarks;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectOrderAndCustomerInformationsBenchmarkMssql : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_MSSQL))]
        public void SetupRepoDbMssql()
        {
            FluentMapper.Entity<SalesOrderHeader>().Table("Sales.SalesOrderHeader");
            FluentMapper.Entity<SalesOrderDetail>().Table("Sales.SalesOrderDetail");
            FluentMapper.Entity<Product>().Table("Production.Product");
            FluentMapper.Entity<Customer>().Table("Sales.Customer");
            FluentMapper.Entity<Person>().Table("Person.Person");
            FluentMapper.Entity<Address>().Table("Person.Address");
        }


        [Benchmark]
        public List<OrderProductDetailDto> Dapper_MSSQL()
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
        public List<OrderProductDetailDto> RepoDb_MSSQL()
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
        public List<OrderProductDetailDto> EFCore_MSSQL_WithJoins()
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

        [Benchmark]
        public List<OrderProductDetailDto> EFCore_MSSQL_IncludeStyle()
        {
            using var context = CreateMssqlContext();
            return context.SalesOrderHeaders
                .Include(soh => soh.SalesOrderDetails)
                .Include(soh => soh.Customer)
                    .ThenInclude(c => c.Person)
                .Include(soh => soh.ShipToAddress)
                .SelectMany(soh => soh.SalesOrderDetails.Select(sod => new OrderProductDetailDto
                {
                    SalesOrderId = soh.SalesOrderId,
                    ProductName = sod.Product.Name,
                    OrderQty = sod.OrderQty,
                    FirstName = soh.Customer.Person.FirstName,
                    LastName = soh.Customer.Person.LastName,
                    AccountNumber = soh.Customer.AccountNumber,
                    City = soh.ShipToAddress.City
                }))
                .OrderBy(x => x.SalesOrderId)
                .ToList();
        }

        [Benchmark]
        public List<OrderProductDetailDto> OrmLite_MSSQL()
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
        public List<OrderProductDetailDto> OrmLite_MSSQL_LinqStyle()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var q = db.From<SalesOrderHeader>()
                .Join<SalesOrderHeader, SalesOrderDetail>((soh, sod) => soh.SalesOrderId == sod.SalesOrderId)
                .Join<SalesOrderDetail, Product>((sod, p) => sod.ProductId == p.ProductId)
                .Join<SalesOrderHeader, Customer>((soh, c) => soh.CustomerId == c.CustomerId)
                .LeftJoin<Customer, Person>((c, pe) => c.CustomerId == pe.BusinessEntityId)
                .Join<SalesOrderHeader, Address>((soh, a) => soh.ShipToAddressId == a.AddressId)
                .Select(@"
                    sales.salesorderheader.salesorderid,
                    production.product.name AS ProductName,
                    sales.salesorderdetail.orderqty,
                    person.person.firstname,
                    person.person.lastname,
                    sales.customer.accountnumber,
                    person.address.city")
                .OrderBy("sales.salesorderheader.salesorderid");

            return db.Select<OrderProductDetailDto>(q);
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
