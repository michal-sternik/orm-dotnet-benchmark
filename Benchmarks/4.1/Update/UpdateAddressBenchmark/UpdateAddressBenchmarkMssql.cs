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
using RepoDb.Enumerations;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class UpdateAddressBenchmarkMssql : OrmBenchmarkBase
    {
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlMssql;

        private List<int> _targetIds;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreateMssqlConnection();

            try
            {
                FluentMapper.Entity<Address>().Table("Person.Address");
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

            _targetIds = conn.Query<int>(
                @"SELECT TOP 100 AddressID FROM Person.Address WHERE AddressLine2 IS NULL"
            ).ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(
                @"UPDATE Person.Address
                  SET AddressLine2 = NULL
                  WHERE AddressID IN @Ids", new { Ids = _targetIds });
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(
                @"UPDATE Person.Address
                  SET AddressLine2 = NULL
                  WHERE AddressID IN @Ids", new { Ids = _targetIds });
        }


        [Benchmark]
        public void Dapper_ORM()
        {

            using var conn = CreateMssqlConnection();
            conn.Execute(
                @"UPDATE Person.Address
                  SET AddressLine2 = 'Undefined'
                  WHERE AddressID IN @Ids", new { Ids = _targetIds });
        }
        [Benchmark]
        public void RepoDb_ORM()
        {
            
            using var conn = CreateMssqlConnection();
            var ids = string.Join(",", _targetIds);
            RepoDb.DbConnectionExtension.ExecuteNonQuery(conn, $@"
                UPDATE Person.Address
                SET AddressLine2 = 'Undefined'
                WHERE AddressID IN ({ids})");
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

            var ids = string.Join(",", _targetIds);
            _sqlSugarClient.Ado.ExecuteCommand($@"
                UPDATE Person.Address
                SET AddressLine2 = 'Undefined'
                WHERE AddressID IN ({ids})");
        }



        [Benchmark]
        public void OrmLite_ORM()
        {
            
            using var db = CreateOrmLiteMssqlConnection();
            var ids = string.Join(",", _targetIds);
            db.ExecuteSql($@"
                UPDATE Person.Address
                SET AddressLine2 = 'Undefined'
                WHERE AddressID IN ({ids})");
        }
        [Benchmark]
        public void FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
            var ids = string.Join(",", _targetIds);
            _freeSqlMssql.Ado.ExecuteNonQuery($@"
                UPDATE Person.Address
                SET AddressLine2 = 'Undefined'
                WHERE AddressID IN ({ids})");
        }
        [Benchmark]
        public void EFCore_ORM()
        {

            using var ctx = CreateMssqlContext();

            var ids = string.Join(",", _targetIds);
            var sql = $@"
                UPDATE Person.Address
                SET AddressLine2 = 'Undefined'
                WHERE AddressID IN ({ids})";

            ctx.Database.ExecuteSqlRaw(sql);
        }

    }
}
