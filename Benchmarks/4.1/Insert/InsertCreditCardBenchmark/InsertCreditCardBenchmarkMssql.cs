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

//czyli wszystkie musza byc w jednym globalsetup, zeby generowac. dodatkowo 
//w iterationsetup losujemy nowe numery kart, bo cos szwankowalo jak byly generowane raz na benchmark w globalsetup
//w iterationcleanup musza byc czyszczenie danych, bo inaczej beda sie nakladac dane z poprzednich iteracji
//trzeba uzupelnic mapowania - robilem to w modelach adnotacjami, ale dla postgresa pewnie bedzie inaczej
//dla niektorych orm dodawac ignore na polach nawigacyjnych i na id trzeba dac autoincrement.
namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class InsertCreditCardBenchmarkMssql : OrmBenchmarkBase
    {
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private List<CreditCard> _creditCards;
        //private List<string> _addedCardNumbers;
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;


        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreateMssqlConnection();

            try
            {
                FluentMapper.Entity<CreditCard>().Table("Sales.CreditCard");
            }
            catch (RepoDb.Exceptions.MappingExistsException)
            {
                // Already mapped
            }

            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);

            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            // Pobierz 10 000 rekordów jako szablon
            var origCards = conn.Query<CreditCard>(
                @"SELECT TOP 1 * FROM Sales.CreditCard"
            ).ToList();

            _creditCards = new List<CreditCard>(origCards.Count);

            // Przekopiuj dane, wyzeruj ID
            foreach (var card in origCards)
            {
                card.CreditCardId = 0;
                _creditCards.Add(card);
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            var rand = new Random();
            //_addedCardNumbers = new List<string>();

            foreach (var cc in _creditCards)
            {
                cc.CardNumber = rand.NextInt64(1_000_000_000_000_000, 9_999_999_999_999_999).ToString();
                cc.CreditCardId = 0;
                //_addedCardNumbers.Add(cc.CardNumber);
            }
        }

        
        [IterationCleanup]
        public void CleanupInserted()
        {
            using var conn = CreateMssqlConnection();
            var cardNumbers = _creditCards.Select(x => x.CardNumber).ToList();
            conn.Execute(@"DELETE FROM Sales.CreditCard WHERE CardNumber IN @Numbers", new { Numbers = cardNumbers });
        }



        [Benchmark]
        public void Dapper_ORM()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(
                @"INSERT INTO Sales.CreditCard (CardType, CardNumber, ExpMonth, ExpYear, ModifiedDate)
                  VALUES (@CardType, @CardNumber, @ExpMonth, @ExpYear, @ModifiedDate)", _creditCards);
        }
        [Benchmark]
        public void RepoDb_ORM()
        {
            using var connection = CreateMssqlConnection();
            RepoDb.DbConnectionExtension.InsertAll(connection, _creditCards);
        }


        [Benchmark]
        public void SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
            _sqlSugarClient.Insertable(_creditCards).ExecuteCommand();
        }
 

        [Benchmark]
        public void OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();
            db.InsertAll(_creditCards);
        }
        [Benchmark]
        public void FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            _freeSqlMssql.Insert<CreditCard>().AppendData(_creditCards).ExecuteAffrows();
        }
        [Benchmark]
        public void EFCore_ORM()
        {
            using var ctx = CreateMssqlContext();
            ctx.CreditCards.AddRange(_creditCards);
            ctx.SaveChanges();
        }

    }
}
