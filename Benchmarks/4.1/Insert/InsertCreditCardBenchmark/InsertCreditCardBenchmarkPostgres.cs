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
using OrmBenchmarkMag.Benchmarks;
using Dm.util;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class InsertCreditCardBenchmarkPostgres : OrmBenchmarkBase
    {
        //tutaj robimy cos takiego, ze wyciagamy max id i probujemy dodac, ale okazuje sie ze mimo ze w bazie
        //max id jest ponad 19k, to przy dodawaniu autoinkrementacja na swiezej bazie i tak liczy od 0,
        //bo postgres przydziela ID na podstawie sekwencji, nie na podstawie danych w tabeli dlatego
        //moj nextval zwracal wartosci ktore juz istnialy w tabeli
        private List<CreditCard> _creditCards;
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;

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
            OrmLiteSchemaConfigurator.ConfigureMappings();

            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);


            var origCards = conn.Query<CreditCard>(
                @"SELECT * FROM sales.creditcard LIMIT 1"
            ).ToList();

            _creditCards = new List<CreditCard>(origCards.Count);

            foreach (var card in origCards)
            {
                _creditCards.Add(new CreditCard
                {
                    CreditCardId = 0, //tymczasowo
                    CardType = card.CardType,
                    CardNumber = card.CardNumber,
                    ExpMonth = card.ExpMonth,
                    ExpYear = card.ExpYear,
                    ModifiedDate = card.ModifiedDate
                });
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreatePostgresConnection();
            var rand = new Random();

            // Pobierz n unikalnych ID z sekwencji PostgreSQL
            var sequenceQuery = $@"SELECT nextval(pg_get_serial_sequence('sales.creditcard', 'creditcardid')) 
                                   FROM generate_series(1, {_creditCards.Count})";

            var newIds = conn.Query<long>(sequenceQuery).ToList();

            for (int i = 0; i < _creditCards.Count; i++)
            {
                _creditCards[i].CardNumber = rand.NextInt64(1_000_000_000_000_000, 9_999_999_999_999_999).ToString();
                _creditCards[i].CreditCardId = (int)newIds[i];
            }
        }

        [IterationCleanup]
        public void CleanupInserted()
        {
            using var conn = CreatePostgresConnection();
            var cardNumbers = _creditCards.Select(x => x.CardNumber).ToList();
            conn.Execute(@"DELETE FROM sales.creditcard WHERE cardnumber = ANY(@Numbers)", new { Numbers = cardNumbers });
        }

        //tutaj nalezy zrobic mapowanie w srodku statementu w benchmarku, bo taki jest priorytet
        //w przypadku orma freesql
        [Benchmark]
        public void FreeSql_Postgres_Insert()
        {
            _freeSqlPostgres.Insert<CreditCard>()
                .AsTable("sales.creditcard")
                .AppendData(_creditCards)
                .ExecuteAffrows();

        }

        [Benchmark]
        public void RepoDb_Postgres_Insert()
        {
            using var connection = CreatePostgresConnection();
            RepoDb.DbConnectionExtension.InsertAll(connection, _creditCards);
        }

        [Benchmark]
        public void Dapper_Postgres_Insert()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"INSERT INTO sales.creditcard (creditcardid, cardtype, cardnumber, expmonth, expyear, modifieddate)
                  VALUES (@CreditCardId, @CardType, @CardNumber, @ExpMonth, @ExpYear, @ModifiedDate)", _creditCards);
        }

        [Benchmark]
        public void EFCore_Postgres_Insert()
        {
            using var ctx = CreatePostgresContext();
            ctx.CreditCards.AddRange(_creditCards);
            ctx.SaveChanges();
        }

        [Benchmark]
        public void OrmLite_Postgres_Insert()
        {
            using var db = CreateOrmLitePostgresConnection();
            db.InsertAll(_creditCards);
        }

        [Benchmark]
        public void SqlSugar_Postgres_Insert()
        {
            _sqlSugarClient.Insertable(_creditCards).ExecuteCommand();
        }
    }
}
