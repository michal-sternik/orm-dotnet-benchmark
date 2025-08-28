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
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        
        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }


        [GlobalSetup(Target = nameof(SqlSugar_ORM))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

        }


        [Benchmark]
        public List<Customer> Dapper_ORM()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<Customer>(@"
                SELECT TOP (1000) *
                FROM Sales.Customer
            ").ToList();
        }


        [Benchmark]
        public List<Customer> RepoDb_ORM()
        {
            using var connection = CreateMssqlConnection();
            return connection.ExecuteQuery<Customer>(@"
                SELECT TOP (1000) *
                FROM Sales.Customer
            ").ToList();
        }

        [Benchmark]
        public List<Customer> SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
            var sql = @"
                SELECT TOP (1000) *
                FROM Sales.Customer";
            return _sqlSugarClient.Ado.SqlQuery<Customer>(sql);
        }

        [Benchmark]
        public List<Customer> OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();
            var sql = @"
                SELECT TOP (1000) *
                FROM Sales.Customer";
            return db.SqlList<Customer>(sql);
        }


        [Benchmark]
        public List<Customer> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
            // Użycie ADO.Query<T> aby wymusić czysty SQL
            return _freeSqlMssql.Ado.Query<Customer>(@"
                SELECT TOP (1000) *
                FROM Sales.Customer
            ").ToList();
        }


        [Benchmark]
        public List<Customer> EFCore_ORM()
        {
            using var context = CreateMssqlContext();


            var query =
                (from c in context.Customers
                 select c)
                .Take(1000);

            return query.ToList();
        }
    }
}
