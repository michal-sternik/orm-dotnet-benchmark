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
        private IFreeSql _freeSqlMssql;

        [GlobalSetup(Target = nameof(FreeSql_MSSQL))]
        public void SetupFreeSqlMssql()
        {
            _freeSqlMssql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> FreeSql_MSSQL()
        {
            return _freeSqlMssql.Ado.Query<ShiftWithEmployeeCountDto>(@"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC
            ").ToList();
        }


        private SqlSugarClient _sqlSugarClient;

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
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
        }

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> SqlSugar_MSSQL()
        {
            var list = _sqlSugarClient.Queryable<EmployeeDepartmentHistory>()
                .LeftJoin<Shift>((edh, s) => edh.ShiftId == s.ShiftId)
                .GroupBy((edh, s) => s.Name)
                .OrderBy("COUNT(edh.BusinessEntityId) DESC")
                .Select<dynamic>("s.Name AS ShiftName, COUNT(edh.BusinessEntityId) AS EmployeeCount")
                .ToList();

            return list.Select(x => new ShiftWithEmployeeCountDto
            {
                ShiftName = x.ShiftName,
                EmployeeCount = Convert.ToInt32(x.EmployeeCount)
            }).ToList();
        }


        [Benchmark]
        public List<ShiftWithEmployeeCountDto> EFCore_MSSQL_WithJoin()
        {
            using var context = CreatePostgresContext();

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

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> EFCore_MSSQL_WithIncludeAndGroup()
        {
            using var context = CreateMssqlContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return context.EmployeeDepartmentHistories
                .Include(e => e.Shift)
                .AsEnumerable()
                .GroupBy(e => e.Shift.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => new ShiftWithEmployeeCountDto
                {
                    ShiftName = g.Key,
                    EmployeeCount = g.Count()
                }).ToList();
        }

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> Dapper_MSSQL()
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
        public List<ShiftWithEmployeeCountDto> RepoDb_MSSQL()
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
        public List<ShiftWithEmployeeCountDto> OrmLite_MSSQL_RawSql()
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
        public List<ShiftWithEmployeeCountDto> OrmLite_MSSQL_LinqStyle()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var q = db.From<EmployeeDepartmentHistory>()
                .Join<Shift>((edh, s) => edh.ShiftId == s.ShiftId)
                .GroupBy("Shift.name")
                .Select("Shift.name AS ShiftName, COUNT(employeedepartmenthistory.businessentityid) AS EmployeeCount")
                .OrderByDescending("COUNT(employeedepartmenthistory.businessentityid)");

            return db.Select<ShiftWithEmployeeCountDto>(q);
        }
    }

    public class ShiftWithEmployeeCountDto
    {
        public string ShiftName { get; set; }
        public int EmployeeCount { get; set; }
    }
}
