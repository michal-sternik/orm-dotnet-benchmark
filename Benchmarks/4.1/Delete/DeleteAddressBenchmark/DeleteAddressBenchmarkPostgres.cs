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
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
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
                @"SELECT * FROM person.address_temp WHERE addressline2 IS NULL LIMIT 100"
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
        public void Dapper_ORM()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(@"DELETE FROM person.address_temp WHERE addressid = ANY(@Ids)",
                new { Ids = _targetAddresses.Select(a => a.AddressId).ToList() });
        }

        [Benchmark]
        public void RepoDb_ORM()
        {
            using var conn = CreatePostgresConnection();

            var ids = _targetAddresses.Select(a => a.AddressId).ToList();

            var idsCsv = string.Join(",", ids);
            RepoDb.DbConnectionExtension.ExecuteNonQuery(conn,$@"DELETE FROM person.address_temp WHERE addressid IN ({string.Join(",", idsCsv)})");
        }

        [Benchmark]
        public void SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
            var ids = _targetAddresses.Select(a => a.AddressId).ToList();

            var idsCsv = string.Join(",", ids);
            _sqlSugarClient.Ado.ExecuteCommand($@"DELETE FROM person.address_temp WHERE addressid IN ({idsCsv})");
        }
        [Benchmark]
        public void OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();

            var ids = _targetAddresses.Select(a => a.AddressId).ToList();
            db.ExecuteSql($"DELETE FROM person.address_temp WHERE addressid IN ({string.Join(",", ids)})");
        }
        [Benchmark]
        public void FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            var ids = _targetAddresses.Select(a => a.AddressId).ToList();


            var idsCsv = string.Join(",", ids);
            _freeSqlPostgres.Ado.ExecuteNonQuery($@"DELETE FROM person.address_temp WHERE addressid IN ({string.Join(",", idsCsv)})");
        }
        [Benchmark]
        public void EFCore_ORM()
        {
            using var ctx = CreatePostgresContext();

            var ids = _targetAddresses.Select(a => a.AddressId).ToList();
            ctx.Database.ExecuteSqlRaw("DELETE FROM person.address_temp WHERE addressid = ANY({0})", ids);
        }
        




    }
}
