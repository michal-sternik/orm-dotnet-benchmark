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
    //tutaj na wzor z postgresowego podejscia - kopia calej tablicy, lepsze niz dodawanie i usuwanie rekordow - omijamy contrainty
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class DeleteCreditCardBenchmarkMssqlTest : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;
        private List<CreditCard> _targetCards;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open();

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

            // Tworzymy tymczasową tabelę
            conn.Execute(@"IF OBJECT_ID('Sales.CreditCard_Temp', 'U') IS NOT NULL DROP TABLE Sales.CreditCard_Temp;");
            conn.Execute(@"SELECT * INTO Sales.CreditCard_Temp FROM Sales.CreditCard WHERE 1 = 0;");

            // Kopiujemy dane do tymczasowej tabeli z IDENTITY_INSERT
            conn.Execute(@"SET IDENTITY_INSERT Sales.CreditCard_Temp ON;");

            conn.Execute(@"INSERT INTO Sales.CreditCard_Temp (CreditCardId, CardType, CardNumber, ExpMonth, ExpYear, ModifiedDate)
                           SELECT CreditCardId, CardType, CardNumber, ExpMonth, ExpYear, ModifiedDate FROM Sales.CreditCard;");

            conn.Execute(@"SET IDENTITY_INSERT Sales.CreditCard_Temp OFF;");

            // Pobieramy dane do usuwania
            _targetCards = conn.Query<CreditCard>(
                @"SELECT TOP 100 * FROM Sales.CreditCard_Temp WHERE CardType = 'Vista'"
            ).ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open();

            conn.Execute(@"DELETE FROM Sales.CreditCard_Temp WHERE CreditCardId IN @Ids",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });

            conn.Execute(@"SET IDENTITY_INSERT Sales.CreditCard_Temp ON;");

            conn.Execute(@"INSERT INTO Sales.CreditCard_Temp (CreditCardId, CardType, CardNumber, ExpMonth, ExpYear, ModifiedDate)
                           SELECT CreditCardId, CardType, CardNumber, ExpMonth, ExpYear, ModifiedDate FROM Sales.CreditCard
                           WHERE CreditCardId IN @Ids",
                           new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });

            conn.Execute(@"SET IDENTITY_INSERT Sales.CreditCard_Temp OFF;");
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // IterationSetup czyści dane na starcie – tu nie trzeba nic robić.
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open();

            conn.Execute(@"IF OBJECT_ID('Sales.CreditCard_Temp', 'U') IS NOT NULL DROP TABLE Sales.CreditCard_Temp;");
        }

        [Benchmark]
        public void FreeSql_MSSQL_Delete()
        {
            _freeSqlMssql.Delete<CreditCard>()
                .AsTable("Sales.CreditCard_Temp")
                .Where(c => _targetCards.Select(x => x.CreditCardId).Contains(c.CreditCardId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_MSSQL_Delete()
        {
            using var conn = CreateMssqlConnection();
            RepoDb.DbConnectionExtension.DeleteAll(conn, "Sales.CreditCard_Temp", _targetCards);
        }

        [Benchmark]
        public void Dapper_MSSQL_Delete()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(@"DELETE FROM Sales.CreditCard_Temp WHERE CreditCardId IN @Ids",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });
        }

        [Benchmark]
        public void EFCore_MSSQL_Delete()
        {
            using var ctx = CreateMssqlContext();

            var ids = _targetCards.Select(c => c.CreditCardId).ToList();
            ctx.Database.ExecuteSqlRaw($"DELETE FROM Sales.CreditCard_Temp WHERE CreditCardId IN ({string.Join(",", ids)})");
        }

        [Benchmark]
        public void OrmLite_MSSQL_Delete()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var ids = _targetCards.Select(c => c.CreditCardId).ToList();
            db.ExecuteSql($"DELETE FROM Sales.CreditCard_Temp WHERE CreditCardId IN ({string.Join(",", ids)})");
        }

        [Benchmark]
        public void SqlSugar_MSSQL_Delete()
        {
            _sqlSugarClient.Deleteable<CreditCard>()
                .AS("Sales.CreditCard_Temp")
                .Where(c => _targetCards.Select(x => x.CreditCardId).Contains(c.CreditCardId))
                .ExecuteCommand();
        }
    }
}
