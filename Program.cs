using System;
using BenchmarkDotNet.Running;
//using Dapper;
using RepoDb;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Data;
using OrmBenchmarkMag.Models;
using OrmBenchmarkThesis.Benchmarks;


namespace OrmBenchmarkMag
{
    class Program
    {
        public static List<SalesOrderHeader> Dapper_SimpleTest()
        {
            GlobalConfiguration.Setup().UseSqlServer();
            FluentMapper.Entity<SalesOrderHeader>()
                .Table("Sales.SalesOrderHeader");

            string MssqlConnectionString = "Server=localhost,1433;Database=AdventureWorks2014;User Id=sa;Password=YourStr0ngP@ssw0rd!;TrustServerCertificate=True";
            using var connection = new SqlConnection(MssqlConnectionString);

            return connection.Query<SalesOrderHeader>(what: null, top: 1000).ToList();


            //return result;
        }

        static void Main(string[] args)
        {
            //var orders = Dapper_SimpleTest();
            //foreach (var o in orders)
            //{
            //    Console.WriteLine(o.SubTotal);
            //}

            //BenchmarkRunner.Run<SelectPeopleBenchmarkMssql>();
            BenchmarkRunner.Run<SelectOrderAndCustomerInformationsBenchmarkMssql>();
        }
    }
}




//[Benchmark]
//public List<Customer> Linq2Db_ManualInclude_MSSQL()
//{
//    using var db = CreateLinq2DbMssqlConnection();

//    var customers = db.GetTable<Customer>()
//        .LoadWith(c => c.Person) 
//        .LoadWith(c => c.SalesOrderHeaders) 
//        .ThenLoad(soh => soh.BillToAddress) 
//        .ThenLoad(addr => addr.StateProvince) 
//        .ToList();

//    return customers;
//}

//nie da sie, bo trzebaby mapowac wszystkie property w encjach jako [column], probowalem to zrobic automatycznie ale sie nie da.
//[Benchmark]
//public List<CustomerWithOrdersDto> Linq2Db_MSSQL()
//{
//    using var db = CreateLinq2DbMssqlConnection();
//    return (from c in db.GetTable<Customer>()
//            join soh in db.GetTable<SalesOrderHeader>() on c.CustomerId equals soh.CustomerId
//            join a in db.GetTable<Address>() on soh.BillToAddressId equals a.AddressId
//            join sp in db.GetTable<StateProvince>() on a.StateProvinceId equals sp.StateProvinceId
//            join p in db.GetTable<Person>() on c.PersonId equals p.BusinessEntityId
//            select new CustomerWithOrdersDto
//            {
//                CustomerID = c.CustomerId,
//                AddressLine1 = a.AddressLine1,
//                StateProvince = sp.Name,
//                FirstName = p.FirstName,
//                LastName = p.LastName
//            }).ToList();
//}