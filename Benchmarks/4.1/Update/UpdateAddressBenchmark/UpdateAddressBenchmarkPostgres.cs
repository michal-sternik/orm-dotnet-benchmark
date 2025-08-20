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
    public class UpdateAddressBenchmarkPostgres : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;

        private List<int> _targetIds;

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

            _targetIds = conn.Query<int>(
                @"SELECT addressid
                  FROM person.address
                  WHERE addressline2 IS NULL
                  LIMIT 10").ToList();
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
        public void FreeSql_Postgres_Update()
        {
            _freeSqlPostgres.Update<Address>()
                .AsTable("person.address")
                .Set(a => a.AddressLine2, "Undefined")
                .Where(a => _targetIds.Contains(a.AddressId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_Postgres_Update()
        {
            using var conn = CreatePostgresConnection();

            var addresses = RepoDb.DbConnectionExtension.Query<Address>(
                conn,
                "person.address",
                where: new RepoDb.QueryField("addressid", RepoDb.Enumerations.Operation.In, _targetIds)).ToList();

            foreach (var address in addresses)
            {
                address.AddressLine2 = "Undefined";
            }

            RepoDb.DbConnectionExtension.UpdateAll(conn, addresses);
        }

        [Benchmark]
        public void Dapper_Postgres_Update()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"UPDATE person.address
                  SET addressline2 = 'Undefined'
                  WHERE addressid = ANY(@Ids)", new { Ids = _targetIds });
        }

        [Benchmark]
        public void EFCore_Postgres_Update()
        {
            using var ctx = CreatePostgresContext();
            ctx.Addresses
               .Where(a => _targetIds.Contains(a.AddressId))
               .ExecuteUpdate(s => s
                   .SetProperty(a => a.AddressLine2, "Undefined"));
        }

        [Benchmark]
        public void OrmLite_Postgres_Update()
        {
            using var db = CreateOrmLitePostgresConnection();
            db.Update<Address>(
                new { AddressLine2 = "Undefined" },
                x => Sql.In(x.AddressId, _targetIds));
        }

        [Benchmark]
        public void SqlSugar_Postgres_Update()
        {
            _sqlSugarClient.Updateable<Address>()
                .SetColumns(a => new Address { AddressLine2 = "Undefined" })
                .Where(a => _targetIds.Contains(a.AddressId))
                .ExecuteCommand();
        }
    }
}
