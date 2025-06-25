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
using RepoDb.Enumerations;
using FreeSql;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class UpdateCreditCardBenchmarkMssql : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        // Lista ID do aktualizacji
        private List<int> _targetIds;

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

            // Pobierz 1000 rekordów z typem 'SuperiorCard'
            _targetIds = conn.Query<int>(
                @"SELECT TOP 100 CreditCardId 
                  FROM Sales.CreditCard 
                  WHERE CardType = 'SuperiorCard'"
            ).ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Przy każdej iteracji, przywracamy dane do oryginału
            using var conn = CreateMssqlConnection();
            conn.Execute(
                @"UPDATE Sales.CreditCard
                  SET CardType = 'SuperiorCard', ExpYear = 2000
                  WHERE CreditCardId IN @Ids", new { Ids = _targetIds });
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // Przywróć dane do oryginału na wszelki wypadek
            using var conn = CreateMssqlConnection();
            conn.Execute(
                @"UPDATE Sales.CreditCard
                  SET CardType = 'SuperiorCard', ExpYear = 2000
                  WHERE CreditCardId IN @Ids", new { Ids = _targetIds });
        }

        [Benchmark]
        public void FreeSql_MSSQL_Update()
        {
            _freeSqlMssql.Update<CreditCard>()
                .Set(a => a.CardType, "Vista")
                .Set(a => a.ExpYear, 2008)
                .Where(a => _targetIds.Contains(a.CreditCardId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_MSSQL_Update()
        {
            using var conn = CreateMssqlConnection();

            // Pobierz encje (to MUSI być w RepoDb dla SQL Server)
            var cards = RepoDb.DbConnectionExtension.Query<CreditCard>(
                conn,
                "Sales.CreditCard",
                where: new QueryField("CreditCardId", Operation.In, _targetIds)).ToList();

            foreach (var card in cards)
            {
                card.CardType = "Vista";
                card.ExpYear = 2008;
            }

            RepoDb.DbConnectionExtension.UpdateAll(conn, cards);
        }

        [Benchmark]
        public void Dapper_MSSQL_Update()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(
                @"UPDATE Sales.CreditCard
                  SET CardType = 'Vista', ExpYear = 2008
                  WHERE CreditCardId IN @Ids", new { Ids = _targetIds });
        }

        [Benchmark]
        public void EFCore_MSSQL_Update()
        {
            using var ctx = CreateMssqlContext();
            ctx.CreditCards
               .Where(c => _targetIds.Contains(c.CreditCardId))
               .ExecuteUpdate(s => s
                   .SetProperty(c => c.CardType, "Vista")
                   .SetProperty(c => c.ExpYear, 2008));
        }

        [Benchmark]
        public void OrmLite_MSSQL_Update()
        {
            using var db = CreateOrmLiteMssqlConnection();
            db.Update<CreditCard>(
                new { CardType = "Vista", ExpYear = 2008 },
                x => Sql.In(x.CreditCardId, _targetIds));
        }

        [Benchmark]
        public void SqlSugar_MSSQL_Update()
        {
            _sqlSugarClient.Updateable<CreditCard>()
                .SetColumns(a => new CreditCard { CardType = "Vista", ExpYear = 2008 })
                .Where(a => _targetIds.Contains(a.CreditCardId))
                .ExecuteCommand();
        }
    }
}
