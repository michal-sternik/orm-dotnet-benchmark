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
using RepoDb.Enumerations;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class UpdateAddressBenchmarkPostgres : OrmBenchmarkBase
    {
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        private List<int> _targetIds;

        private IFreeSql _freeSqlPostgres;

        [GlobalSetup(Target = nameof(FreeSql))]
        public void SetupFreeSql()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreatePostgresConnection();

            try
            {
                RepoDbSchemaConfigurator.Init();
            }
            catch (RepoDb.Exceptions.MappingExistsException)
            {
                // Already mapped
            }

            OrmLiteSchemaConfigurator.ConfigureMappings();

            _targetIds = conn.Query<int>(
                @"SELECT addressid
                  FROM person.address
                  WHERE addressline2 IS NULL
                  LIMIT 1000").ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"UPDATE person.address
                  SET addressline2 = NULL
                  WHERE addressid = ANY(@Ids)", new { Ids = _targetIds });
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"UPDATE person.address
                  SET addressline2 = NULL
                  WHERE addressid = ANY(@Ids)", new { Ids = _targetIds });
        }

        [Benchmark]
        public void Dapper_ORM()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"UPDATE person.address
                  SET addressline2 = 'Undefined'
                  WHERE addressid = ANY(@Ids)", new { Ids = _targetIds });
        }

        [Benchmark]
        public void RepoDb_ORM()
        {
            using var conn = CreatePostgresConnection();

            var ids = string.Join(",", _targetIds);
            RepoDb.DbConnectionExtension.ExecuteNonQuery(conn, $@"
                UPDATE person.address
                SET addressline2 = 'Undefined'
                WHERE addressid IN ({ids})");
        }

        [Benchmark]
        public void SqlSugar_ORM()
        {
            using var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(db);
            var ids = string.Join(",", _targetIds);
            var sql = $@"
                UPDATE person.address
                SET addressline2 = 'Undefined'
                WHERE addressid IN ({ids})";

            db.Ado.ExecuteCommand(sql);
        }

        [Benchmark]
        public void OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();

            var ids = string.Join(",", _targetIds);
            var sql = $@"
                UPDATE person.address
                SET addressline2 = 'Undefined'
                WHERE addressid IN ({ids})";

            db.ExecuteSql(sql);
        }

        [Benchmark]
        public void FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);

            _freeSqlPostgres.Update<Address>()
                .AsTable("person.address")
                .Set(a => a.AddressLine2, "Undefined")
                .Where(a => _targetIds.Contains(a.AddressId))
                .ExecuteAffrows();
        }



        [Benchmark]
        public void EFCore_ORM()
        {
            using var ctx = CreatePostgresContext();

            var ids = string.Join(",", _targetIds);
            var sql = $@"
                UPDATE person.address
                SET addressline2 = 'Undefined'
                WHERE addressid IN ({ids})";

            ctx.Database.ExecuteSqlRaw(sql);
        }


    }
}
