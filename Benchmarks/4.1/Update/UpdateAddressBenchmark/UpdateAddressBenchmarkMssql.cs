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
    public class UpdateAddressBenchmarkMssql : OrmBenchmarkBase
    {
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
                @"SELECT TOP 10 AddressID FROM Person.Address WHERE AddressLine2 IS NULL"
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
        public void FreeSql_MSSQL_Update()
        {
            _freeSqlMssql.Update<Address>()
                .Set(a => a.AddressLine2, "Undefined")
                .Where(a => _targetIds.Contains(a.AddressId))
                .ExecuteAffrows();
        }

        [Benchmark]
        public void RepoDb_MSSQL_Update()
        {
            using var conn = CreateMssqlConnection();

            var addresses = RepoDb.DbConnectionExtension.Query<Address>(
                conn,
                "Person.Address",
                where: new RepoDb.QueryField("AddressID", RepoDb.Enumerations.Operation.In, _targetIds)).ToList();

            foreach (var address in addresses)
            {
                address.AddressLine2 = "Undefined";
            }

            RepoDb.DbConnectionExtension.UpdateAll(conn, addresses);
        }

        [Benchmark]
        public void Dapper_MSSQL_Update()
        {
            using var conn = CreateMssqlConnection();
            conn.Execute(
                @"UPDATE Person.Address
                  SET AddressLine2 = 'Undefined'
                  WHERE AddressID IN @Ids", new { Ids = _targetIds });
        }

        [Benchmark]
        public void EFCore_MSSQL_Update()
        {
            using var ctx = CreateMssqlContext();
            ctx.Addresses
               .Where(a => _targetIds.Contains(a.AddressId))
               .ExecuteUpdate(s => s
                   .SetProperty(a => a.AddressLine2, "Undefined"));
        }

        [Benchmark]
        public void OrmLite_MSSQL_Update()
        {
            using var db = CreateOrmLiteMssqlConnection();
            db.Update<Address>(
                new { AddressLine2 = "Undefined" },
                x => Sql.In(x.AddressId, _targetIds));
        }

        [Benchmark]
        public void SqlSugar_MSSQL_Update()
        {
            _sqlSugarClient.Updateable<Address>()
                .SetColumns(a => new Address { AddressLine2 = "Undefined" })
                .Where(a => _targetIds.Contains(a.AddressId))
                .ExecuteCommand();
        }
    }
}
