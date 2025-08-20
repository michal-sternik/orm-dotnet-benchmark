using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using ServiceStack.OrmLite;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Benchmarks;
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class NumberOfPeopleOnShiftsBenchmarkPostgres : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDb() => RepoDbSchemaConfigurator.Init();

        [GlobalSetup(Target = nameof(OrmLite_Postgres_LinqStyle))]
        public void SetupOrmLite() => OrmLiteSchemaConfigurator.ConfigureMappings();

        private IFreeSql _freeSqlPostgres;

        [GlobalSetup(Target = nameof(FreeSql_Postgres))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> FreeSql_Postgres()
        {
            return _freeSqlPostgres.Ado.Query<ShiftWithEmployeeCountDto>(@"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC
            ").ToList();
        }


        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(SqlSugar_Postgres))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
        }

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> SqlSugar_Postgres()
        {
            var list = _sqlSugarClient.Queryable<EmployeeDepartmentHistory>()
                .LeftJoin<Shift>((edh, s) => edh.ShiftId == s.ShiftId)
                .GroupBy((edh, s) => s.Name)
                .OrderBy("COUNT(edh.businessentityid) DESC")
                .Select<dynamic>("s.name AS shiftname, COUNT(edh.businessentityid) AS employeecount")
                .ToList();

            return list.Select(x => new ShiftWithEmployeeCountDto
            {
                ShiftName = x.shiftname,
                EmployeeCount = Convert.ToInt32(x.employeecount)
            }).ToList();
        }


        [Benchmark]
        public List<ShiftWithEmployeeCountDto> EFCore_Postgres_WithJoin()
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
        public List<ShiftWithEmployeeCountDto> Dapper_Postgres()
        {
            using var connection = CreatePostgresConnection();

            var sql = @"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC;";

            return connection.Query<ShiftWithEmployeeCountDto>(sql).ToList();
        }

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> RepoDb_Postgres()
        {
            using var connection = CreatePostgresConnection();

            var sql = @"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC;";

            return connection.ExecuteQuery<ShiftWithEmployeeCountDto>(sql).ToList();
        }

        [Benchmark]
        public List<ShiftWithEmployeeCountDto> OrmLite_Postgres_LinqStyle()
        {
            using var db = CreateOrmLitePostgresConnection();

            var q = db.From<EmployeeDepartmentHistory>()
                .Join<Shift>((edh, s) => edh.ShiftId == s.ShiftId)
                .GroupBy("shift.name")
                .Select("shift.name AS ShiftName, COUNT(employeedepartmenthistory.businessentityid) AS EmployeeCount")
                .OrderByDescending("COUNT(employeedepartmenthistory.businessentityid)");

            return db.Select<ShiftWithEmployeeCountDto>(q);
        }
    }
}
