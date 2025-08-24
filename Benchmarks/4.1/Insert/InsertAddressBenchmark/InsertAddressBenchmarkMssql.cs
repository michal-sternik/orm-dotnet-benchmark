using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using ServiceStack.OrmLite;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using OrmBenchmarkMag.Benchmarks;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class InsertAddressBenchmarkMssql : OrmBenchmarkBase
    {
        private List<Address> _addresses;
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreateMssqlConnection();

            try
            {
                FluentMapper.Entity<Address>().Table("Person.Address");
            }
            catch (RepoDb.Exceptions.MappingExistsException)
            {
                // Already mapped
            }

            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);

            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            var origAddresses = conn.Query<Address>(
                @"SELECT TOP 100 * FROM Person.Address"
            ).ToList();

            _addresses = new List<Address>(origAddresses.Count);

            foreach (var addr in origAddresses)
            {
                addr.AddressId = 0;
                _addresses.Add(addr);
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            var rand = new Random();

            foreach (var addr in _addresses)
            {
                addr.AddressLine1 = "Test Street " + rand.NextInt64(1_000_000_000_000_000, 9_999_999_999_999_999).ToString();
                addr.PostalCode = rand.Next(10000, 99999).ToString();
                addr.Rowguid = Guid.NewGuid(); // Generujemy unikalny rowguid
                addr.AddressId = 0;
            }
        }

        [IterationCleanup]
        public void CleanupInserted()
        {
            using var conn = CreateMssqlConnection();
            var streets = _addresses.Select(x => x.AddressLine1).ToList();
            conn.Execute(@"DELETE FROM Person.Address WHERE AddressLine1 IN @Streets", new { Streets = streets });
        }

        [Benchmark]
        public void FreeSql_MSSQL_Insert()
        {
            _freeSqlMssql.Insert<Address>().AppendData(_addresses).ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_MSSQL_Insert()
        {
            using var connection = CreateMssqlConnection();
            RepoDb.DbConnectionExtension.InsertAll(connection, _addresses);
        }


        //dapper robi bulkinsert nawet 8 sekund dlatego ze bulkinsert tutaj to tak naprawde pojedyncze inserty
        //zeby bylo szybciej trzebaby uzyc zewnetrznej biblioteki do dappera
        [Benchmark]
        public void Dapper_MSSQL_Insert()
        {
            using var conn = CreateMssqlConnection();

            conn.Execute(
                @"INSERT INTO Person.Address (AddressLine1, AddressLine2, City, StateProvinceID, PostalCode,SpatialLocation, Rowguid, ModifiedDate)
                  VALUES (@AddressLine1, @AddressLine2, @City, @StateProvinceID, @PostalCode, NULL, @rowguid, @ModifiedDate)", _addresses);
        }

        [Benchmark]
        public void EFCore_MSSQL_Insert()
        {
            using var ctx = CreateMssqlContext();
            ctx.Addresses.AddRange(_addresses);
            ctx.SaveChanges();
        }

        [Benchmark]
        public void OrmLite_MSSQL_Insert()
        {
            using var db = CreateOrmLiteMssqlConnection();
            db.InsertAll(_addresses);
        }

        [Benchmark]
        public void SqlSugar_MSSQL_Insert()
        {
            _sqlSugarClient.Insertable(_addresses).ExecuteCommand();
        }
    }
}
