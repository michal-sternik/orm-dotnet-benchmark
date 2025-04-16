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

            BenchmarkRunner.Run<SelectPeopleBenchmark>();
        }
    }
}
