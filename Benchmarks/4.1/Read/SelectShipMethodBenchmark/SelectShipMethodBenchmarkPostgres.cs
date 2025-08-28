using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Config;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Benchmarks;
using ServiceStack.OrmLite;
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectShipMethodBenchmarkPostgres : OrmBenchmarkBase
    {
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;

        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDbPostgres()
        {
            RepoDbSchemaConfigurator.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_ORM))]
        public void SetupOrmLiteMappings()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        [GlobalSetup(Target = nameof(SqlSugar_ORM))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
        }

        [Benchmark]
        public List<ShipMethod> Dapper_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.Query<ShipMethod>(@"
                SELECT *
                FROM purchasing.shipmethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> RepoDb_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<ShipMethod>(@"
                SELECT *
                FROM purchasing.shipmethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
            var sql = @"SELECT * FROM purchasing.shipmethod";
            return _sqlSugarClient.Ado.SqlQuery<ShipMethod>(sql);
        }

        [Benchmark]
        public List<ShipMethod> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();
            return db.SqlList<ShipMethod>(@"SELECT * FROM purchasing.shipmethod");
        }

        [Benchmark]
        public List<ShipMethod> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<ShipMethod>(@"
                SELECT *
                FROM purchasing.shipmethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> EFCore_ORM()
        {
            using var context = CreatePostgresContext();

            var query =
                from sm in context.ShipMethods
                select sm;

            return query.ToList();
        }
    }
}
