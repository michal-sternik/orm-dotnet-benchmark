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
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
        }

        
        [Benchmark]
        public List<Department> Dapper_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.Query<Department>(@"
                SELECT *
                FROM humanresources.department
            ").ToList();
        }

       
        [Benchmark]
        public List<Department> RepoDb_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<Department>(@"
                SELECT *
                FROM humanresources.department
            ").ToList();
        }

     
        [Benchmark]
        public List<Department> SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
            var sql = @"
                SELECT *
                FROM humanresources.department";
            return _sqlSugarClient.Ado.SqlQuery<Department>(sql);
        }

      
        [Benchmark]
        public List<Department> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();
            var sql = @"
                SELECT *
                FROM humanresources.department";
            return db.SqlList<Department>(sql);
        }

    
        [Benchmark]
        public List<Department> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<Department>(@"
                SELECT *
                FROM humanresources.department
            ").ToList();
        }

     
        [Benchmark]
        public List<Department> EFCore_ORM()
        {
            using var context = CreatePostgresContext();

            var query =
                from d in context.Departments
                select d;

            return query.ToList();
        }
    }
}
