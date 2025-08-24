using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using ServiceStack.OrmLite;
using SqlSugar;
using System.Collections.Generic;
using System.Linq;
using FreeSql;
using OrmBenchmarkMag.Benchmarks;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class DeleteAddressBenchmarkMssql : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;
        private List<Address> _targetAddresses;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open();

            try { FluentMapper.Entity<Address>().Table("Person.Address"); }
            catch { }

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

            // Tworzymy tymczasową tabelę
            conn.Execute(@"IF OBJECT_ID('Person.Address_Temp', 'U') IS NOT NULL DROP TABLE Person.Address_Temp;");
            conn.Execute(@"SELECT * INTO Person.Address_Temp FROM Person.Address WHERE 1 = 0;");

            conn.Execute(@"SET IDENTITY_INSERT Person.Address_Temp ON;");

            conn.Execute(@"INSERT INTO Person.Address_Temp (AddressID, AddressLine1, AddressLine2, City, StateProvinceID, PostalCode, SpatialLocation, RowGuid, ModifiedDate)
                           SELECT AddressID, AddressLine1, AddressLine2, City, StateProvinceID, PostalCode, SpatialLocation, RowGuid, ModifiedDate FROM Person.Address;");

            conn.Execute(@"SET IDENTITY_INSERT Person.Address_Temp OFF;");

            _targetAddresses = conn.Query<Address>(
                @"SELECT TOP 1000 * FROM Person.Address_Temp WHERE AddressLine2 IS NULL"
            ).ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open();

            conn.Execute(@"DELETE FROM Person.Address_Temp WHERE AddressID IN @Ids",
                new { Ids = _targetAddresses.Select(a => a.AddressId).ToList() });

            conn.Execute(@"SET IDENTITY_INSERT Person.Address_Temp ON;");

            conn.Execute(@"INSERT INTO Person.Address_Temp (AddressID, AddressLine1, AddressLine2, City, StateProvinceID, PostalCode, SpatialLocation, RowGuid, ModifiedDate)
                           SELECT AddressID, AddressLine1, AddressLine2, City, StateProvinceID, PostalCode, SpatialLocation, RowGuid, ModifiedDate FROM Person.Address
                           WHERE AddressID IN @Ids", new { Ids = _targetAddresses.Select(a => a.AddressId).ToList() });

            conn.Execute(@"SET IDENTITY_INSERT Person.Address_Temp OFF;");
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open();

            conn.Execute(@"IF OBJECT_ID('Person.Address_Temp', 'U') IS NOT NULL DROP TABLE Person.Address_Temp;");
        }

        [Benchmark]
        public void FreeSql_MSSQL_Delete()
        {
            _freeSqlMssql.Delete<Address>()
                .AsTable("Person.Address_Temp")
                .Where(a => _targetAddresses.Select(x => x.AddressId).Contains(a.AddressId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_MSSQL_Delete()
        {
            using var conn = CreateMssqlConnection();
            RepoDb.DbConnectionExtension.DeleteAll(conn, "Person.Address_Temp", _targetAddresses);
        }

        [Benchmark]
        public void Dapper_MSSQL_Delete()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(@"DELETE FROM Person.Address_Temp WHERE AddressID IN @Ids",
                new { Ids = _targetAddresses.Select(a => a.AddressId).ToList() });
        }

        [Benchmark]
        public void EFCore_MSSQL_Delete()
        {
            using var ctx = CreateMssqlContext();

            var ids = _targetAddresses.Select(a => a.AddressId).ToList();
            ctx.Database.ExecuteSqlRaw($"DELETE FROM Person.Address_Temp WHERE AddressID IN ({string.Join(",", ids)})");
        }

        [Benchmark]
        public void OrmLite_MSSQL_Delete()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var ids = _targetAddresses.Select(a => a.AddressId).ToList();
            db.ExecuteSql($"DELETE FROM Person.Address_Temp WHERE AddressID IN ({string.Join(",", ids)})");
        }

        [Benchmark]
        public void SqlSugar_MSSQL_Delete()
        {
            _sqlSugarClient.Deleteable<Address>()
                .AS("Person.Address_Temp")
                .Where(a => _targetAddresses.Select(x => x.AddressId).Contains(a.AddressId))
                .ExecuteCommand();
        }
    }
}
