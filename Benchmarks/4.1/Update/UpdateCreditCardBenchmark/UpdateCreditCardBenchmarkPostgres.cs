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
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;

        // Lista ID do aktualizacji
        private List<int> _targetIds;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreatePostgresConnection();

            try
            {
                PostgresRepoDbMappingSetup.Init();
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

            // Pobierz 1000 rekordów z typem 'SuperiorCard'
            _targetIds = conn.Query<int>(
                @"SELECT creditcardid
                  FROM sales.creditcard
                  WHERE cardtype = 'SuperiorCard'
                  LIMIT 100").ToList();
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
        public void FreeSql_Postgres_Update()
        {
            _freeSqlPostgres.Update<CreditCard>()
                .AsTable("sales.creditcard")
                .Set(a => a.CardType, "Vista")
                .Set(a => a.ExpYear, 2008)
                .Where(a => _targetIds.Contains(a.CreditCardId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_Postgres_Update()
        {
            using var conn = CreatePostgresConnection();

            // Pobierz encje (to MUSI być w RepoDb dla SQL Server)
            var cards = RepoDb.DbConnectionExtension.Query<CreditCard>(
                conn,
                "sales.creditcard",
                where: new QueryField("creditcardid", Operation.In, _targetIds)).ToList();

            foreach (var card in cards)
            {
                card.CardType = "Vista";
                card.ExpYear = 2008;
            }

            RepoDb.DbConnectionExtension.UpdateAll(conn, cards);
        }

        [Benchmark]
        public void Dapper_Postgres_Update()
        {
            using var conn = CreatePostgresConnection();
            conn.Execute(
                @"UPDATE sales.creditcard
                  SET cardtype = 'Vista', expyear = 2008
                  WHERE creditcardid = ANY(@Ids)", new { Ids = _targetIds });
        }

        [Benchmark]
        public void EFCore_Postgres_Update()
        {
            using var ctx = CreatePostgresContext();
            ctx.CreditCards
               .Where(c => _targetIds.Contains(c.CreditCardId))
               .ExecuteUpdate(s => s
                   .SetProperty(c => c.CardType, "Vista")
                   .SetProperty(c => c.ExpYear, 2008));
        }

        [Benchmark]
        public void OrmLite_Postgres_Update()
        {
            using var db = CreateOrmLitePostgresConnection();
            db.Update<CreditCard>(
                new { CardType = "Vista", ExpYear = 2008 },
                x => Sql.In(x.CreditCardId, _targetIds));
        }

        [Benchmark]
        public void SqlSugar_Postgres_Update()
        {
            _sqlSugarClient.Updateable<CreditCard>()
                .SetColumns(a => new CreditCard { CardType = "Vista", ExpYear = 2008 })
                .Where(a => _targetIds.Contains(a.CreditCardId))
                .ExecuteCommand();
        }
    }
}
