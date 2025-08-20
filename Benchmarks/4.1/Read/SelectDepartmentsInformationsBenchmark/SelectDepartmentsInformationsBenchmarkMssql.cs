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
    public class SelectDepartmentsInformationsBenchmarkMssql : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        // RepoDb – jeśli masz centralny init mapowań, wywołaj go tutaj
        [GlobalSetup(Target = nameof(RepoDb_MSSQL))]
        public void SetupRepoDbMssql()
        {
            RepoDbSchemaConfigurator.Init();
        }

        // FreeSql
        [GlobalSetup(Target = nameof(FreeSql_MSSQL))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        // SqlSugar
        [GlobalSetup(Target = nameof(SqlSugar_MSSQL))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            // Jeśli używasz własnych mapowań:
            // SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
        }

        // RAW SQL – Dapper
        [Benchmark]
        public List<Department> Dapper_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<Department>(@"
                SELECT *
                FROM HumanResources.Department
            ").ToList();
        }

        // RAW SQL – RepoDb
        [Benchmark]
        public List<Department> RepoDb_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.ExecuteQuery<Department>(@"
                SELECT *
                FROM HumanResources.Department
            ").ToList();
        }

        // RAW SQL – SqlSugar
        [Benchmark]
        public List<Department> SqlSugar_MSSQL()
        {
            var sql = @"
                SELECT *
                FROM HumanResources.Department";
            return _sqlSugarClient.Ado.SqlQuery<Department>(sql);
        }

        // RAW SQL – OrmLite
        [Benchmark]
        public List<Department> OrmLite_MSSQL()
        {
            using var db = CreateOrmLiteMssqlConnection();
            var sql = @"
                SELECT *
                FROM HumanResources.Department";
            return db.SqlList<Department>(sql);
        }

        // RAW SQL – FreeSql (ADO)
        [Benchmark]
        public List<Department> FreeSql_MSSQL()
        {
            return _freeSqlMssql.Ado.Query<Department>(@"
                SELECT *
                FROM HumanResources.Department
            ").ToList();
        }

        // EF Core – LINQ query syntax (bez Include, bez method syntax)
        [Benchmark]
        public List<Department> EFCore_MSSQL()
        {
            using var context = CreateMssqlContext();

            var query =
                from d in context.Departments
                select d;

            return query.ToList();
        }
    }
}
