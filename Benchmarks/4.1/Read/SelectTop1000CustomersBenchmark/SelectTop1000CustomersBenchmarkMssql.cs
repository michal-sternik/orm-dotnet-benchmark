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
    public class SelectTop1000CustomersBenchmarkMssql : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        // --- FreeSql setup (dla raw ADO.Query) ---
        [GlobalSetup(Target = nameof(FreeSql_MSSQL))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        // --- SqlSugar setup (wspólne dla raw) ---
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
            // Jeśli masz własne mapowania, możesz je tu zawołać:
            // SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
        }

        // -------------------------
        // RAW SQL – Dapper
        // -------------------------
        [Benchmark]
        public List<Customer> Dapper_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<Customer>(@"
                SELECT TOP (1000) *
                FROM Sales.Customer
            ").ToList();
        }

        // -------------------------
        // RAW SQL – RepoDb
        // -------------------------
        [Benchmark]
        public List<Customer> RepoDb_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.ExecuteQuery<Customer>(@"
                SELECT TOP (1000) *
                FROM Sales.Customer
            ").ToList();
        }

        // -------------------------
        // RAW SQL – SqlSugar
        // -------------------------
        [Benchmark]
        public List<Customer> SqlSugar_MSSQL()
        {
            var sql = @"
                SELECT TOP (1000) *
                FROM Sales.Customer";
            return _sqlSugarClient.Ado.SqlQuery<Customer>(sql);
        }

        // -------------------------
        // RAW SQL – OrmLite
        // -------------------------
        [Benchmark]
        public List<Customer> OrmLite_MSSQL()
        {
            using var db = CreateOrmLiteMssqlConnection();
            var sql = @"
                SELECT TOP (1000) *
                FROM Sales.Customer";
            return db.SqlList<Customer>(sql);
        }

        // -------------------------
        // RAW SQL – FreeSql (ADO)
        // -------------------------
        [Benchmark]
        public List<Customer> FreeSql_MSSQL()
        {
            // Użycie ADO.Query<T> aby wymusić czysty SQL
            return _freeSqlMssql.Ado.Query<Customer>(@"
                SELECT TOP (1000) *
                FROM Sales.Customer
            ").ToList();
        }

        // -------------------------
        // EF Core – LINQ query syntax (comprehension)
        // -------------------------
        [Benchmark]
        public List<Customer> EFCore_MSSQL()
        {
            using var context = CreateMssqlContext();

            // Styl 'from ... in ... select ...' (comprehension). Tu nie ma joinów,
            // bo selekcjonujemy wyłącznie Customers; celowo bez Include i bez method syntax.
            var query =
                (from c in context.Customers
                 select c)
                .Take(1000);

            return query.ToList();
        }
    }
}
