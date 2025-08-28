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
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDbMssql()
        {
            RepoDbSchemaConfigurator.Init();
        }

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        [GlobalSetup(Target = nameof(SqlSugar_ORM))]
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
        public List<ShipMethod> Dapper_ORM()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<ShipMethod>(@"
                SELECT *
                FROM Purchasing.ShipMethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> RepoDb_ORM()
        {
            using var connection = CreateMssqlConnection();
            return connection.ExecuteQuery<ShipMethod>(@"
                SELECT *
                FROM Purchasing.ShipMethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
            var sql = @"SELECT * FROM Purchasing.ShipMethod";
            return _sqlSugarClient.Ado.SqlQuery<ShipMethod>(sql);
        }

        [Benchmark]
        public List<ShipMethod> OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();
            return db.SqlList<ShipMethod>(@"SELECT * FROM Purchasing.ShipMethod");
        }

        [Benchmark]
        public List<ShipMethod> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
            return _freeSqlMssql.Ado.Query<ShipMethod>(@"
                SELECT *
                FROM Purchasing.ShipMethod
            ").ToList();
        }

        [Benchmark]
        public List<ShipMethod> EFCore_ORM()
        {
            using var context = CreateMssqlContext();

            var query =
                from sm in context.ShipMethods
                select sm;

            return query.ToList();
        }
    }
}
