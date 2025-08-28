using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using RepoDb.Enumerations;
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
    public class UpdateCreditCardBenchmarkPostgres : OrmBenchmarkBase
    {
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
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

            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            OrmLiteSchemaConfigurator.ConfigureMappings();
            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);

            _targetIds = conn.Query<int>(
                @"SELECT creditcardid
                  FROM sales.creditcard
                  WHERE cardtype = 'SuperiorCard'
                  LIMIT 100").ToList() ?? new List<int>();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            

            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"UPDATE sales.creditcard
                  SET cardtype = 'SuperiorCard', expyear = 2000
                  WHERE creditcardid = ANY(@Ids)", new { Ids = _targetIds });
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            

            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"UPDATE sales.creditcard
                  SET cardtype = 'SuperiorCard', expyear = 2000
                  WHERE creditcardid = ANY(@Ids)", new { Ids = _targetIds });
        }




        [Benchmark]
        public void Dapper_ORM()
        {
            

            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"UPDATE sales.creditcard
                  SET cardtype = 'Vista', expyear = 2008
                  WHERE creditcardid = ANY(@Ids)", new { Ids = _targetIds });
        }
        [Benchmark]
        public void RepoDb_ORM()
        {


            using var conn = CreatePostgresConnection();
            RepoDb.DbConnectionExtension.ExecuteNonQuery(conn,
            @"UPDATE sales.creditcard
              SET cardtype = 'Vista', expyear = 2008
              WHERE creditcardid IN (@Ids);",
            new { Ids = _targetIds.ToArray() });


        }

        [Benchmark]
        public void SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);

            var ids = string.Join(",", _targetIds);
            _sqlSugarClient.Ado.ExecuteCommand($@"
                UPDATE sales.creditcard
                SET cardtype = 'Vista', expyear = 2008
                WHERE creditcardid IN ({ids})");
        }


        [Benchmark]
        public void OrmLite_ORM()
        {
            

            using var db = CreateOrmLitePostgresConnection();
            var ids = string.Join(",", _targetIds);
            db.ExecuteSql($@"
                UPDATE sales.creditcard
                SET cardtype = 'Vista', expyear = 2008
                WHERE creditcardid IN ({ids})");
        }
        [Benchmark]
        public void FreeSql_ORM()
        {

            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            var ids = string.Join(",", _targetIds);
            _freeSqlPostgres.Ado.ExecuteNonQuery($@"
                UPDATE sales.creditcard
                SET cardtype = 'Vista', expyear = 2008
                WHERE creditcardid IN ({ids})");
        }

        [Benchmark]
        public void EFCore_ORM()
        {


            using var ctx = CreatePostgresContext();
            var ids = string.Join(",", _targetIds);
            var sql = $@"
                UPDATE sales.creditcard
                SET cardtype = 'Vista', expyear = 2008
                WHERE creditcardid IN ({ids})";

            ctx.Database.ExecuteSqlRaw(sql);
        }

    }
}
