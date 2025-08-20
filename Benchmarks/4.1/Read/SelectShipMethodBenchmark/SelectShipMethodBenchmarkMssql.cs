using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Config;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Benchmarks;
using ServiceStack.OrmLite;
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectShipMethodBenchmarkMssql : OrmBenchmarkBase
    {
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        [GlobalSetup(Target = nameof(RepoDb_MSSQL))]
        public void SetupRepoDbMssql()
        {
            RepoDbSchemaConfigurator.Init();
        }

        [GlobalSetup(Target = nameof(FreeSql_MSSQL))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        [GlobalSetup(Target = nameof(SqlSugar_MSSQL))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
        }

        [Benchmark]
        public List<ShipMethod> Dapper_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<ShipMethod>(@"
                SELECT *
                FROM Purchasing.ShipMethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> RepoDb_MSSQL()
        {
            using var connection = CreateMssqlConnection();
            return connection.ExecuteQuery<ShipMethod>(@"
                SELECT *
                FROM Purchasing.ShipMethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> SqlSugar_MSSQL()
        {
            var sql = @"SELECT * FROM Purchasing.ShipMethod";
            return _sqlSugarClient.Ado.SqlQuery<ShipMethod>(sql);
        }

        [Benchmark]
        public List<ShipMethod> OrmLite_MSSQL()
        {
            using var db = CreateOrmLiteMssqlConnection();
            return db.SqlList<ShipMethod>(@"SELECT * FROM Purchasing.ShipMethod");
        }

        [Benchmark]
        public List<ShipMethod> FreeSql_MSSQL()
        {
            return _freeSqlMssql.Ado.Query<ShipMethod>(@"
                SELECT *
                FROM Purchasing.ShipMethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> EFCore_MSSQL()
        {
            using var context = CreateMssqlContext();

            var query =
                from sm in context.ShipMethods
                select sm;

            return query.ToList();
        }
    }
}
