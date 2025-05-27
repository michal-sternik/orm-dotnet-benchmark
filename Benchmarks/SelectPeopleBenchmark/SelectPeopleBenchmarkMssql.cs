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
    public class SelectPeopleBenchmarkMssql : OrmBenchmarkBase

    {


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

        [Benchmark]
        public List<SalesOrderHeader> EFCore_MSSQL()
        {
            using var context = CreateMssqlContext();
            return context.SalesOrderHeaders.ToList();
        }

        [Benchmark]
        public List<SalesOrderHeader> Dapper_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<SalesOrderHeader>("SELECT * FROM Sales.SalesOrderHeader").ToList();
        }

        [Benchmark]
        public List<SalesOrderHeader> RepoDb_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.QueryAll<SalesOrderHeader>().ToList();
        }

       
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

//roznica miedzy "debug" a "release"

//| Method | Mean | Error | StdDev | Gen0 | Gen1 | Gen2 | Allocated |
//| ---------------- | ---------:| ---------:| --------:| -----------:| ----------:| ----------:| ----------:|
//| EFCore_Postgres | 251.7 ms | 6.68 ms | 4.42 ms | 10500.0000 | 5700.0000 | 1000.0000 | 60.64 MB |
//| EFCore_MSSQL | 280.9 ms | 8.26 ms | 5.46 ms | 10300.0000 | 5500.0000 | 800.0000 | 60.68 MB |
//| Dapper_MSSQL | 152.5 ms | 6.29 ms | 3.74 ms | 5700.0000 | 3200.0000 | 600.0000 | 31.07 MB |
//| Dapper_Postgres | 120.4 ms | 13.90 ms | 9.19 ms | 5700.0000 | 3100.0000 | 500.0000 | 31.78 MB |

//| Method | Mean | Error | StdDev | Gen0 | Gen1 | Gen2 | Allocated |
//| ---------------- | ---------:| --------:| --------:| -----------:| ----------:| ----------:| ----------:|
//| EFCore_Postgres | 233.0 ms | 5.75 ms | 3.01 ms | 10600.0000 | 5900.0000 | 1100.0000 | 60.64 MB |
//| EFCore_MSSQL | 256.6 ms | 9.17 ms | 4.79 ms | 10500.0000 | 5800.0000 | 1000.0000 | 60.68 MB |
//| Dapper_MSSQL | 131.9 ms | 3.82 ms | 2.53 ms | 5500.0000 | 3000.0000 | 500.0000 | 31.07 MB |
//| Dapper_Postgres | 108.6 ms | 2.25 ms | 1.34 ms | 5700.0000 | 3100.0000 | 500.0000 | 31.78 MB |