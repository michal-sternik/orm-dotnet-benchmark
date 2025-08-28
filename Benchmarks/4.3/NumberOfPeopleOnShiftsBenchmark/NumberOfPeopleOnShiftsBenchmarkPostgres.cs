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
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDb() => RepoDbSchemaConfigurator.Init();

        [GlobalSetup(Target = nameof(OrmLite_ORM))]
        public void SetupOrmLite() => OrmLiteSchemaConfigurator.ConfigureMappings();

        private IFreeSql _freeSqlPostgres;

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }



        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(SqlSugar_ORM))]
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
        public List<ShiftWithEmployeeCountDto> Dapper_ORM()
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
        public List<ShiftWithEmployeeCountDto> RepoDb_ORM()
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
        public List<ShiftWithEmployeeCountDto> SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
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
            using var db = CreateOrmLitePostgresConnection();

            var sql = @"
                SELECT s.name AS ShiftName, COUNT(edh.businessentityid) AS EmployeeCount
                FROM humanresources.employeedepartmenthistory edh
                JOIN humanresources.shift s ON edh.shiftid = s.shiftid
                GROUP BY s.name
                ORDER BY EmployeeCount DESC;";

            return db.SqlList<ShiftWithEmployeeCountDto>(sql);
        }
        [Benchmark]
        public List<ShiftWithEmployeeCountDto> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<ShiftWithEmployeeCountDto>(@"
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
    }
}
