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
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;
        private List<CreditCard> _targetCards;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();

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
            OrmLiteSchemaConfigurator.ConfigureMappings();

            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);

            // Tworzymy tymczasową tabelę (kopia struktury)
            conn.Execute(@"DROP TABLE IF EXISTS sales.creditcard_temp;");
            // Tworzenie tabeli z pełną strukturą
            conn.Execute(@"DROP TABLE IF EXISTS sales.creditcard_temp;");
            conn.Execute(@"CREATE TABLE sales.creditcard_temp (LIKE sales.creditcard INCLUDING ALL);");

            // Usuwanie constraintów FK
            conn.Execute(@"ALTER TABLE sales.creditcard_temp DROP CONSTRAINT IF EXISTS ""FK_PersonCreditCard_CreditCard_CreditCardID"";");
            conn.Execute(@"ALTER TABLE sales.creditcard_temp DROP CONSTRAINT IF EXISTS ""FK_SalesOrderHeader_CreditCard_CreditCardID"";");



            // Kopiujemy dane do tymczasowej tabeli
            conn.Execute(@"INSERT INTO sales.creditcard_temp SELECT * FROM sales.creditcard;");

            // Pobieramy dane do usuwania
            _targetCards = conn.Query<CreditCard>(
                @"SELECT * FROM sales.creditcard_temp WHERE cardtype = 'Vista' LIMIT 100"
            ).ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();

            // Usuwamy ewentualne pozostałości z poprzedniej iteracji
            conn.Execute(@"DELETE FROM sales.creditcard_temp WHERE creditcardid = ANY(@Ids)",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });

            // Kopiujemy rekordy benchmarkowe z oryginalnej tabeli do tymczasowej
            conn.Execute(@"INSERT INTO sales.creditcard_temp SELECT * FROM sales.creditcard WHERE creditcardid = ANY(@Ids)",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // Tu nic nie musisz robić, bo IterationSetup i tak czyści dane na starcie.
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            using var conn = CreatePostgresConnection();
            conn.Open();

            // Usuwamy tymczasową tabelę po zakończeniu benchmarków
            conn.Execute(@"DROP TABLE IF EXISTS sales.creditcard_temp;");
        }

        [Benchmark]
        public void FreeSql_Postgres_Delete()
        {
            _freeSqlPostgres.Delete<CreditCard>()
                .AsTable("sales.creditcard_temp")
                .Where(c => _targetCards.Select(x => x.CreditCardId).Contains(c.CreditCardId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_Postgres_Delete()
        {
            using var conn = CreatePostgresConnection();
            RepoDb.DbConnectionExtension.DeleteAll(conn, "sales.creditcard_temp", _targetCards);
        }

        [Benchmark]
        public void Dapper_Postgres_Delete()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(@"DELETE FROM sales.creditcard_temp WHERE creditcardid = ANY(@Ids)",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });
        }

        //[Benchmark]
        //public void EFCore_Postgres_Delete()
        //{
        //    using var ctx = CreatePostgresContext();
        //    var cards = ctx.CreditCards
        //        .FromSqlRaw("SELECT * FROM sales.creditcard_temp")
        //        .Where(c => _targetCards.Select(x => x.CreditCardId).Contains(c.CreditCardId))
        //        .ToList();

        //    ctx.CreditCards.RemoveRange(cards);
        //    ctx.SaveChanges();
        //}
        [Benchmark]
        public void EFCore_Postgres_Delete()
        {
            using var ctx = CreatePostgresContext();

            var ids = _targetCards.Select(c => c.CreditCardId).ToList();

            ctx.Database.ExecuteSqlRaw("DELETE FROM sales.creditcard_temp WHERE creditcardid = ANY({0})", ids);
        }


        [Benchmark]
        public void OrmLite_Postgres_Delete()
        {
            using var db = CreateOrmLitePostgresConnection();

            var ids = _targetCards.Select(c => c.CreditCardId).ToList();
            db.ExecuteSql($"DELETE FROM sales.creditcard_temp WHERE creditcardid IN ({string.Join(",", ids)})");
        }




        [Benchmark]
        public void SqlSugar_Postgres_Delete()
        {
            _sqlSugarClient.Deleteable<CreditCard>()
                .AS("sales.creditcard_temp")
                .Where(c => _targetCards.Select(x => x.CreditCardId).Contains(c.CreditCardId))
                .ExecuteCommand();
        }
    }
}
