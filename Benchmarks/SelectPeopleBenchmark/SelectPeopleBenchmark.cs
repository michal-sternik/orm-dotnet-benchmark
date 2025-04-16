using BenchmarkDotNet.Attributes;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Benchmarks;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using RepoDb;


namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectPeopleBenchmark : OrmBenchmarkBase

    {
        //robimy tylko przed pierwszym benchmarkiem RepoDB, bo mapowania nie mozna wykonac kilkukrotnie
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDBPostgres()
        {
            //dla RepoDB trzeba zmapować model na pakiet.model
            //FluentMapper.Entity<SalesOrderHeader>()
            //        .Table("sales.salesorderheader")
            //        .Column(e => e.SalesOrderId, "salesorderid");
            RepoDbMappingSetup.Init();
        }

        [GlobalCleanup (Target = nameof(RepoDb_Postgres))]
        public void CleanupRepoDBPostgres()
        {
            //dla RepoDB trzeba zmapować model na pakiet.model
            //FluentMapper.Entity<SalesOrderHeader>()
            //        .Table("sales.salesorderheader")
            //        .Column(e => e.SalesOrderId, "salesorderid");
            //RepoDbMappingSetup.Init();
            RepoDb.FluentMapper.Entity<SalesOrderHeader>()
                    .Table("sales.salesorderheader")
                    .// czyli niema wyjscia. trzeba to zrobic w dwoch roznych plikach (np rozdzielic w zaleznosci od uzywanej bazy jesli chcemy repodb. nie mozna fluentmappera w jednym uruchomieniu uzyc 2 razy


        }

        [GlobalSetup(Target = nameof(RepoDb_MSSQL))]
        public void SetupRepoDbMssql()
        {
            //dla RepoDB trzeba zmapować model na pakiet.model
            FluentMapper.Entity<SalesOrderHeader>()
                    .Table("Sales.SalesOrderHeader");
                    

        }
        //dla queries zwracajacych wiecej rekordow roznica miedzy mssql a postgres sie zaciera - chyba
        //[Params(100, 1000)] 
        //public int NumberOfRecords;

        //[Benchmark]
        //public List<SalesOrderHeader> EFCore_Postgres()
        //{
        //    using var context = CreatePostgresContext();
        //    return context.SalesOrderHeaders.ToList();
        //}

        //[Benchmark]
        //public List<SalesOrderHeader> EFCore_MSSQL()
        //{
        //    using var context = CreateMssqlContext();
        //    return context.SalesOrderHeaders.ToList();
        //}

        //[Benchmark]
        //public List<SalesOrderHeader> Dapper_MSSQL()
        //{
        //    using var connection = CreateMssqlConnection();
        //    return connection.Query<SalesOrderHeader>("SELECT * FROM Sales.SalesOrderHeader").ToList();
        //}

        //[Benchmark]
        //public List<SalesOrderHeader> Dapper_Postgres()
        //{
        //    using var connection = CreatePostgresConnection();
        //    return connection.Query<SalesOrderHeader>("SELECT * FROM Sales.SalesOrderHeader").ToList();
        //}

        [Benchmark]
        public List<SalesOrderHeader> RepoDb_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.QueryAll<SalesOrderHeader>().ToList();
        }
        [Benchmark]
        public List<SalesOrderHeader> RepoDb_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.QueryAll<SalesOrderHeader>().ToList();
        }





        //[Benchmark]
        //public List<Person> RawSql_MSSQL() =>
        //    _mssqlContext.People.FromSqlRaw($"SELECT TOP {NumberOfRecords} * FROM Person.Person").ToList();

        //[Benchmark]
        //public List<Person> RawSql_Postgres() =>
        //    _postgresContext.People.FromSqlRaw($"SELECT * FROM Person.Person LIMIT {NumberOfRecords}").ToList();
    }
}


//| Method | NumberOfRecords | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
//| ---------------- | ---------------- | ------------:| ----------:| ----------:| --------:| -------:| ----------:|
//| EFCore_MSSQL | 100 | 2,173.2 us | 90.13 us | 53.64 us | 7.0000 | - | 36.4 KB |
//| EFCore_Postgres | 100 | 940.3 us | 35.78 us | 18.71 us | 6.0000 | - | 31.75 KB |
//| RawSql_MSSQL | 100 | 2,075.1 us | 47.18 us | 31.20 us | 8.0000 | - | 39.14 KB |
//| RawSql_Postgres | 100 | 923.7 us | 62.68 us | 41.46 us | 7.0000 | - | 34.43 KB |
//| EFCore_MSSQL | 1000 | 11,003.2 us | 758.13 us | 501.46 us | 58.0000 | 4.0000 | 268.52 KB |
//| EFCore_Postgres | 1000 | 3,447.6 us | 525.52 us | 347.60 us | 57.0000 | 3.0000 | 263.82 KB |
//| RawSql_MSSQL | 1000 | 10,686.1 us | 574.84 us | 342.08 us | 59.0000 | 4.0000 | 271.26 KB |
//| RawSql_Postgres | 1000 | 3,287.1 us | 497.21 us | 328.87 us | 58.0000 | 3.0000 | 266.51 KB |


//| Method | NumberOfRecords | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
//| ---------------- | ---------------- | ------------:| ------------:| ------------:| --------:| -------:| ----------:|
//| EFCore_Postgres | 100 | 0.9136 ms | 31.12 us | 18.52 us | 6.0000 | - | 31.74 KB |
//| EFCore_MSSQL | 100 | 2.9693 ms | 1,726.19 us | 1,141.77 us | 7.0000 | - | 36.4 KB |
//| EFCore_Postgres | 1000 | 2.8473 ms | 311.31 us | 185.25 us | 57.0000 | 3.0000 | 263.81 KB |
//| EFCore_MSSQL | 1000 | 10.0391 ms | 445.86 us | 294.91 us | 58.0000 | 4.0000 | 268.51 KB |
//| EFCore_Postgres | x100 | 1.595 ms | 0.1109 ms | 0.0734 ms | 61.0000 | 20.0000 | 297.88 KB |
//| EFCore_MSSQL | x100 | 3.271 ms | 0.0640 ms | 0.0423 ms | 511.0000 | 135.0000 | 2411.94 KB |
//| EFCore_Postgres | x1000 | 5.754 ms | 0.3812 ms | 0.2268 ms | 425.0000 | 213.0000 | 2517.27 KB |
//| EFCore_MSSQL | x1000 | 16.454 ms | 0.1307 ms | 0.0778 ms | 3954.0000 | 512.0000 | 23687.72 KB |