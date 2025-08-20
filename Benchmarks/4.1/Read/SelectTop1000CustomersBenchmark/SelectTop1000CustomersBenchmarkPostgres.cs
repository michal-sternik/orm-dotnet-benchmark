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
    public class SelectTop1000CustomersBenchmarkPostgres : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;

        // --- RepoDb: ewentualne mapowania/schemat ---
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDbPostgres()
        {
            RepoDbSchemaConfigurator.Init();
        }

        // --- OrmLite: mapowania (jeśli masz własne) ---
        [GlobalSetup(Target = nameof(OrmLite_Postgres))]
        public void SetupOrmLiteMappings()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }

        // --- FreeSql setup ---
        [GlobalSetup(Target = nameof(FreeSql_Postgres))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        // --- SqlSugar setup ---
        [GlobalSetup(Target = nameof(SqlSugar_Postgres))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
        }

        // -------------------------
        // RAW SQL – Dapper
        // -------------------------
        [Benchmark]
        public List<Customer> Dapper_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.Query<Customer>(@"
                SELECT *
                FROM sales.customer
                LIMIT 1000
            ").ToList();
        }

        // -------------------------
        // RAW SQL – RepoDb
        // -------------------------
        [Benchmark]
        public List<Customer> RepoDb_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<Customer>(@"
                SELECT *
                FROM sales.customer
                LIMIT 1000
            ").ToList();
        }

        // -------------------------
        // RAW SQL – SqlSugar
        // -------------------------
        [Benchmark]
        public List<Customer> SqlSugar_Postgres()
        {
            var sql = @"
                SELECT *
                FROM sales.customer
                LIMIT 1000";
            return _sqlSugarClient.Ado.SqlQuery<Customer>(sql);
        }

        // -------------------------
        // RAW SQL – OrmLite
        // -------------------------
        [Benchmark]
        public List<Customer> OrmLite_Postgres()
        {
            using var db = CreateOrmLitePostgresConnection();
            var sql = @"
                SELECT *
                FROM sales.customer
                LIMIT 1000";
            return db.SqlList<Customer>(sql);
        }

        // -------------------------
        // RAW SQL – FreeSql (ADO)
        // -------------------------
        [Benchmark]
        public List<Customer> FreeSql_Postgres()
        {
            return _freeSqlPostgres.Ado.Query<Customer>(@"
                SELECT *
                FROM sales.customer
                LIMIT 1000
            ").ToList();
        }

        // -------------------------
        // EF Core – LINQ query syntax (comprehension)
        // -------------------------
        [Benchmark]
        public List<Customer> EFCore_Postgres()
        {
            using var context = CreatePostgresContext();

            // Styl 'from ... in ... select ...' (bez Include, bez method syntax).
            var query =
                (from c in context.Customers
                 select c)
                .Take(1000);

            return query.ToList();
        }
    }
}
