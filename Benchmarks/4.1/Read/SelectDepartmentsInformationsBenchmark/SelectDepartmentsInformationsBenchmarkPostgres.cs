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
    public class SelectDepartmentsInformationsBenchmarkPostgres : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;

        // RepoDb – init schem/mapping
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDbPostgres()
        {
            RepoDbSchemaConfigurator.Init();
        }

        // OrmLite – jeśli masz własne mapowania do schematów
        [GlobalSetup(Target = nameof(OrmLite_Postgres))]
        public void SetupOrmLiteMappings()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }

        // FreeSql
        [GlobalSetup(Target = nameof(FreeSql_Postgres))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        // SqlSugar
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

        // RAW SQL – Dapper
        [Benchmark]
        public List<Department> Dapper_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.Query<Department>(@"
                SELECT *
                FROM humanresources.department
            ").ToList();
        }

        // RAW SQL – RepoDb
        [Benchmark]
        public List<Department> RepoDb_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<Department>(@"
                SELECT *
                FROM humanresources.department
            ").ToList();
        }

        // RAW SQL – SqlSugar
        [Benchmark]
        public List<Department> SqlSugar_Postgres()
        {
            var sql = @"
                SELECT *
                FROM humanresources.department";
            return _sqlSugarClient.Ado.SqlQuery<Department>(sql);
        }

        // RAW SQL – OrmLite
        [Benchmark]
        public List<Department> OrmLite_Postgres()
        {
            using var db = CreateOrmLitePostgresConnection();
            var sql = @"
                SELECT *
                FROM humanresources.department";
            return db.SqlList<Department>(sql);
        }

        // RAW SQL – FreeSql (ADO)
        [Benchmark]
        public List<Department> FreeSql_Postgres()
        {
            return _freeSqlPostgres.Ado.Query<Department>(@"
                SELECT *
                FROM humanresources.department
            ").ToList();
        }

        // EF Core – LINQ query syntax
        [Benchmark]
        public List<Department> EFCore_Postgres()
        {
            using var context = CreatePostgresContext();

            var query =
                from d in context.Departments
                select d;

            return query.ToList();
        }
    }
}
