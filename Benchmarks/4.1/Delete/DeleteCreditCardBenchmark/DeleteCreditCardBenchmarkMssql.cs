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
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;
        private List<CreditCard> _targetCards;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreateMssqlConnection();
            conn.Open();

            try { FluentMapper.Entity<CreditCard>().Table("Sales.CreditCard"); } catch { }

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
            // intentionally empty
        }


        [Benchmark]
        public void Dapper_ORM()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(@"DELETE FROM Sales.CreditCard WHERE CreditCardId IN @Ids",
                new { Ids = _targetCards.Select(c => c.CreditCardId).ToList() });
        }
        [Benchmark]
        public void RepoDb_ORM()
        {
            using var conn = CreateMssqlConnection();
            var idsCsv = string.Join(",", _targetCards.Select(a => a.CreditCardId));
            RepoDb.DbConnectionExtension.ExecuteNonQuery(conn, $@"DELETE FROM Sales.CreditCard WHERE CreditCardId IN ({idsCsv})");
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
            var idsCsv = string.Join(",", _targetCards.Select(a => a.CreditCardId));
            _sqlSugarClient.Ado.ExecuteCommand($@"DELETE FROM Sales.CreditCard WHERE CreditCardId IN ({idsCsv})");
        }



        [Benchmark]
        public void OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();
            var ids = _targetCards.Select(a => a.CreditCardId).ToList();
            db.ExecuteSql($"DELETE FROM Sales.CreditCard WHERE CreditCardId IN ({string.Join(",", ids)})");
        }
        [Benchmark]
        public void FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
            var idsCsv = string.Join(",", _targetCards.Select(a => a.CreditCardId));
            _freeSqlMssql.Ado.ExecuteNonQuery($@"DELETE FROM Sales.CreditCard WHERE CreditCardId IN ({idsCsv})");
        }
        [Benchmark]
        public void EFCore_ORM()
        {
            using var ctx = CreateMssqlContext();
            var ids = _targetCards.Select(a => a.CreditCardId).ToList();
            ctx.Database.ExecuteSqlRaw($"DELETE FROM Sales.CreditCard WHERE CreditCardId IN ({string.Join(",", ids)})");
        }

    }
}
