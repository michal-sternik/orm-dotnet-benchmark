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
    public class DeleteCreditCardBenchmarkPostgres : OrmBenchmarkBase
    {
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;
        private List<CreditCard> _targetCards;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();

            try { RepoDbSchemaConfigurator.Init(); } catch { }

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

            conn.Execute(@"DROP TABLE IF EXISTS sales.creditcard_temp;");
            conn.Execute(@"CREATE TABLE sales.creditcard_temp (LIKE sales.creditcard INCLUDING ALL);");

            // na wszelki wypadek – usuń FKi, jeśli LIKE je skopiował
            conn.Execute(@"ALTER TABLE sales.creditcard_temp DROP CONSTRAINT IF EXISTS ""FK_PersonCreditCard_CreditCard_CreditCardID"";");
            conn.Execute(@"ALTER TABLE sales.creditcard_temp DROP CONSTRAINT IF EXISTS ""FK_SalesOrderHeader_CreditCard_CreditCardID"";");

            conn.Execute(@"INSERT INTO sales.creditcard_temp SELECT * FROM sales.creditcard;");

            _targetCards = conn.Query<CreditCard>(
                @"SELECT * FROM sales.creditcard_temp WHERE cardtype = 'Vista' LIMIT 100"
            ).ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();

            conn.Execute(@"DELETE FROM sales.creditcard_temp WHERE creditcardid = ANY(@Ids)",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });

            conn.Execute(@"INSERT INTO sales.creditcard_temp SELECT * FROM sales.creditcard WHERE creditcardid = ANY(@Ids)",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // intentionally empty
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();
            conn.Execute(@"DROP TABLE IF EXISTS sales.creditcard_temp;");
        }


        [Benchmark]
        public void Dapper_ORM()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(@"DELETE FROM sales.creditcard_temp WHERE creditcardid = ANY(@Ids)",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });
        }
        [Benchmark]
        public void RepoDb_ORM()
        {
            using var conn = CreatePostgresConnection();
            var idsCsv = string.Join(",", _targetCards.Select(a => a.CreditCardId));
            RepoDb.DbConnectionExtension.ExecuteNonQuery(conn, $@"DELETE FROM sales.creditcard_temp WHERE creditcardid IN ({idsCsv})");
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
            var idsCsv = string.Join(",", _targetCards.Select(a => a.CreditCardId));
            _sqlSugarClient.Ado.ExecuteCommand($@"DELETE FROM sales.creditcard_temp WHERE creditcardid IN ({idsCsv})");
        }



        [Benchmark]
        public void OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();
            var ids = _targetCards.Select(c => c.CreditCardId).ToList();
            db.ExecuteSql($"DELETE FROM sales.creditcard_temp WHERE creditcardid IN ({string.Join(",", ids)})");
        }
        [Benchmark]
        public void FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            var idsCsv = string.Join(",", _targetCards.Select(a => a.CreditCardId));
            _freeSqlPostgres.Ado.ExecuteNonQuery($@"DELETE FROM sales.creditcard_temp WHERE creditcardid IN ({idsCsv})");
        }
        [Benchmark]
        public void EFCore_ORM()
        {
            using var ctx = CreatePostgresContext();
            var ids = _targetCards.Select(c => c.CreditCardId).ToList();
            ctx.Database.ExecuteSqlRaw("DELETE FROM sales.creditcard_temp WHERE creditcardid = ANY({0})", ids);
        }

    }
}
