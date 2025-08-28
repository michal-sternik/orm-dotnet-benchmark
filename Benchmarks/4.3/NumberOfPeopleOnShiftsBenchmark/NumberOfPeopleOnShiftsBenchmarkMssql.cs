using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using RepoDb;
using ServiceStack.OrmLite;
using SqlSugar;

namespace OrmBenchmarkMag.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class NumberOfPeopleOnShiftsBenchmarkMssql : OrmBenchmarkBase
    {
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private IFreeSql _freeSqlMssql;

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        private SqlSugarClient _sqlSugarClient;

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
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
        }




        [Benchmark]
        public List<ShiftWithEmployeeCountDto> Dapper_ORM()
        {
            using var connection = CreateMssqlConnection();

            var sql = @"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC;";

            return connection.Query<ShiftWithEmployeeCountDto>(sql).ToList();
        }

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> RepoDb_ORM()
        {
            using var connection = CreateMssqlConnection();

            var sql = @"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC;";

            return connection.ExecuteQuery<ShiftWithEmployeeCountDto>(sql).ToList();
        }
        [Benchmark]
        public List<ShiftWithEmployeeCountDto> SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = MssqlConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
            var sql = @"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC";
            return _sqlSugarClient.Ado.SqlQuery<ShiftWithEmployeeCountDto>(sql);
        }
        [Benchmark]
        public List<ShiftWithEmployeeCountDto> OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var sql = @"
                SELECT s.Name AS ShiftName, COUNT(edh.BusinessEntityID) AS EmployeeCount
                FROM HumanResources.EmployeeDepartmentHistory edh
                JOIN HumanResources.Shift s ON edh.ShiftID = s.ShiftID
                GROUP BY s.Name
                ORDER BY EmployeeCount DESC;";

            return db.SqlList<ShiftWithEmployeeCountDto>(sql);
        }
        [Benchmark]
        public List<ShiftWithEmployeeCountDto> FreeSql_ORM()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
             .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
             .UseAutoSyncStructure(false)
             .Build();
            return _freeSqlMssql.Ado.Query<ShiftWithEmployeeCountDto>(@"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC
            ").ToList();
        }
        [Benchmark]
        public List<ShiftWithEmployeeCountDto> EFCore_ORM()
        {
            using var context = CreateMssqlContext();

            return (from edh in context.EmployeeDepartmentHistories
                    join s in context.Shifts on edh.ShiftId equals s.ShiftId
                    group edh by s.Name into g
                    orderby g.Count() descending
                    select new ShiftWithEmployeeCountDto
                    {
                        ShiftName = g.Key,
                        EmployeeCount = g.Count()
                    }).ToList();
        }


    }

    public class ShiftWithEmployeeCountDto
    {
        public string ShiftName { get; set; }
        public int EmployeeCount { get; set; }
    }
}
