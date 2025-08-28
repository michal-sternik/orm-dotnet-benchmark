using System;
using BenchmarkDotNet.Running;
//using Dapper;
using RepoDb;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Data;
using OrmBenchmarkMag.Models;
using OrmBenchmarkThesis.Benchmarks;
using OrmBenchmarkMag.Benchmarks;


namespace OrmBenchmarkMag
{
    class Program
    {
        

        static void Main(string[] args)
        {
  
            BenchmarkRunner.Run<ProductsOrderedMostOftenSortedBenchmarkPostgres>();
        }
    }
}

