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
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDbMssql()
        {
            RepoDbSchemaConfigurator.Init();
        }

       
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
        public List<Department> Dapper_ORM()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<Department>(@"
                SELECT *
                FROM HumanResources.Department
            ").ToList();
        }

      
        [Benchmark]
        public List<Department> RepoDb_ORM()
        {
            using var connection = CreateMssqlConnection();
            return connection.ExecuteQuery<Department>(@"
                SELECT *
                FROM HumanResources.Department
            ").ToList();
        }

       
        [Benchmark]
        public List<Department> SqlSugar_ORM()
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
                SELECT *
                FROM HumanResources.Department";
            return _sqlSugarClient.Ado.SqlQuery<Department>(sql);
        }

       
        [Benchmark]
        public List<Department> OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();
            var sql = @"
                SELECT *
                FROM HumanResources.Department";
            return db.SqlList<Department>(sql);
        }

      
        [Benchmark]
        public List<Department> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
            return _freeSqlMssql.Ado.Query<Department>(@"
                SELECT *
                FROM HumanResources.Department
            ").ToList();
        }

       
        [Benchmark]
        public List<Department> EFCore_ORM()
        {
            using var context = CreateMssqlContext();

            var query =
                from d in context.Departments
                select d;

            return query.ToList();
        }
    }
}
