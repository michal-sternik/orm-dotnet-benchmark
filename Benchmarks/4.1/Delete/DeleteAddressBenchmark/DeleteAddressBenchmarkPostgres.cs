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
    public class DeleteAddressBenchmarkPostgres : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;
        private List<Address> _targetAddresses;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();

            try { RepoDbSchemaConfigurator.Init(); }
            catch { }

            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
            OrmLiteSchemaConfigurator.ConfigureMappings();

            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);

            conn.Execute(@"DROP TABLE IF EXISTS person.address_temp;");
            conn.Execute(@"CREATE TABLE person.address_temp (LIKE person.address INCLUDING ALL);");

            conn.Execute(@"INSERT INTO person.address_temp SELECT * FROM person.address;");

            _targetAddresses = conn.Query<Address>(
                @"SELECT * FROM person.address_temp WHERE addressline2 IS NULL LIMIT 10"
            ).ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();

            conn.Execute(@"DELETE FROM person.address_temp WHERE addressid = ANY(@Ids)",
                new { Ids = _targetAddresses.Select(a => a.AddressId).ToList() });

            conn.Execute(@"INSERT INTO person.address_temp SELECT * FROM person.address WHERE addressid = ANY(@Ids)",
                new { Ids = _targetAddresses.Select(a => a.AddressId).ToList() });
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();

            conn.Execute(@"DROP TABLE IF EXISTS person.address_temp;");
        }

        [Benchmark]
        public void FreeSql_Postgres_Delete()
        {
            _freeSqlPostgres.Delete<Address>()
                .AsTable("person.address_temp")
                .Where(a => _targetAddresses.Select(x => x.AddressId).Contains(a.AddressId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_Postgres_Delete()
        {
            using var conn = CreatePostgresConnection();
            RepoDb.DbConnectionExtension.DeleteAll(conn, "person.address_temp", _targetAddresses);
        }

        [Benchmark]
        public void Dapper_Postgres_Delete()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(@"DELETE FROM person.address_temp WHERE addressid = ANY(@Ids)",
                new { Ids = _targetAddresses.Select(a => a.AddressId).ToList() });
        }

        [Benchmark]
        public void EFCore_Postgres_Delete()
        {
            using var ctx = CreatePostgresContext();

            var ids = _targetAddresses.Select(a => a.AddressId).ToList();
            ctx.Database.ExecuteSqlRaw("DELETE FROM person.address_temp WHERE addressid = ANY({0})", ids);
        }

        [Benchmark]
        public void OrmLite_Postgres_Delete()
        {
            using var db = CreateOrmLitePostgresConnection();

            var ids = _targetAddresses.Select(a => a.AddressId).ToList();
            db.ExecuteSql($"DELETE FROM person.address_temp WHERE addressid IN ({string.Join(",", ids)})");
        }

        [Benchmark]
        public void SqlSugar_Postgres_Delete()
        {
            _sqlSugarClient.Deleteable<Address>()
                .AS("person.address_temp")
                .Where(a => _targetAddresses.Select(x => x.AddressId).Contains(a.AddressId))
                .ExecuteCommand();
        }
    }
}
