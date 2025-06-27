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
    public class DeleteCreditCardBenchmarkMssql : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;
        private List<CreditCard> _targetCards;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open(); // WAŻNE - trzymamy połączenie

            try { FluentMapper.Entity<CreditCard>().Table("Sales.CreditCard"); }
            catch { }

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

            _targetCards = conn.Query<CreditCard>(
                @"SELECT TOP 100 * FROM Sales.CreditCard WHERE CardType = 'Vista'"
            ).ToList();

            conn.Execute(@"ALTER TABLE Sales.PersonCreditCard NOCHECK CONSTRAINT FK_PersonCreditCard_CreditCard_CreditCardID;");
            conn.Execute(@"ALTER TABLE Sales.SalesOrderHeader NOCHECK CONSTRAINT FK_SalesOrderHeader_CreditCard_CreditCardID;");

            conn.Execute(@"DELETE FROM Sales.CreditCard WHERE CreditCardId IN @Ids",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });

            conn.Execute(@"SET IDENTITY_INSERT Sales.CreditCard ON;");

            foreach (var card in _targetCards)
            {
                conn.Execute(
                    @"INSERT INTO Sales.CreditCard (CreditCardId, CardType, CardNumber, ExpMonth, ExpYear, ModifiedDate)
                      VALUES (@CreditCardId, @CardType, @CardNumber, @ExpMonth, @ExpYear, @ModifiedDate)", card);
            }

            conn.Execute(@"SET IDENTITY_INSERT Sales.CreditCard OFF;");
            conn.Execute(@"ALTER TABLE Sales.PersonCreditCard CHECK CONSTRAINT FK_PersonCreditCard_CreditCard_CreditCardID;");
            conn.Execute(@"ALTER TABLE Sales.SalesOrderHeader CHECK CONSTRAINT FK_SalesOrderHeader_CreditCard_CreditCardID;");
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open();

            conn.Execute(@"ALTER TABLE Sales.SalesOrderHeader NOCHECK CONSTRAINT FK_SalesOrderHeader_CreditCard_CreditCardID;");
            conn.Execute(@"ALTER TABLE Sales.PersonCreditCard NOCHECK CONSTRAINT FK_PersonCreditCard_CreditCard_CreditCardID;");

            // Usuwasz stare dane, nawet jeśli nie było cleanupu
            conn.Execute(@"DELETE FROM Sales.CreditCard WHERE CreditCardId IN @Ids",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });

            conn.Execute(@"SET IDENTITY_INSERT Sales.CreditCard ON;");

            foreach (var card in _targetCards)
            {
                conn.Execute(
                    @"INSERT INTO Sales.CreditCard (CreditCardId, CardType, CardNumber, ExpMonth, ExpYear, ModifiedDate)
              VALUES (@CreditCardId, @CardType, @CardNumber, @ExpMonth, @ExpYear, @ModifiedDate)", card);
            }

            conn.Execute(@"SET IDENTITY_INSERT Sales.CreditCard OFF;");
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // Możesz to całkowicie usunąć lub zostawić puste.
        }


        [Benchmark]
        public void FreeSql_MSSQL_Delete()
        {
            _freeSqlMssql.Delete<CreditCard>()
                .Where(c => _targetCards.Select(x => x.CreditCardId).Contains(c.CreditCardId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_MSSQL_Delete()
        {
            using var conn = CreateMssqlConnection();
            RepoDb.DbConnectionExtension.DeleteAll<CreditCard>(conn, _targetCards);
        }

        [Benchmark]
        public void Dapper_MSSQL_Delete()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(@"DELETE FROM Sales.CreditCard WHERE CreditCardId IN @Ids",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });
        }

        [Benchmark]
        public void EFCore_MSSQL_Delete()
        {
            using var ctx = CreateMssqlContext();
            var cards = ctx.CreditCards
                .Where(c => _targetCards.Select(x => x.CreditCardId).Contains(c.CreditCardId))
                .ToList();

            ctx.CreditCards.RemoveRange(cards);
            ctx.SaveChanges();
        }

        [Benchmark]
        public void OrmLite_MSSQL_Delete()
        {
            using var db = CreateOrmLiteMssqlConnection();
            ServiceStack.OrmLite.OrmLiteWriteExpressionsApi.Delete<CreditCard>(db, c => Sql.In(c.CreditCardId, _targetCards.Select(x => x.CreditCardId).ToList()));
        }

        [Benchmark]
        public void SqlSugar_MSSQL_Delete()
        {
            _sqlSugarClient.Deleteable<CreditCard>()
                .Where(c => _targetCards.Select(x => x.CreditCardId).Contains(c.CreditCardId))
                .ExecuteCommand();
        }
    }
}

