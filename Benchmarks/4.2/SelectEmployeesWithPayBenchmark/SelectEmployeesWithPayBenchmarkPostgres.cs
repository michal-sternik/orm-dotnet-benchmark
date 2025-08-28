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
using FreeSql;

namespace OrmBenchmarkThesis.Benchmarks
{

    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectEmployeesWithPayBenchmarkPostgres : OrmBenchmarkBase
    {
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDbPostgres()
        {
            RepoDbSchemaConfigurator.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_ORM))]
        public void SetupOrmLiteMappings()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }

        private IFreeSql _freeSqlPostgres;

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }



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
        public List<EmployeeWithPayDto> Dapper_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.Query<EmployeeWithPayDto>(
                @"
                    SELECT e.businessentityid,
                           p.firstname,
                           p.lastname,
                           e.jobtitle,
                           d.name AS department,
                           ep.rate
                    FROM humanresources.employee e
                    JOIN humanresources.employeepayhistory ep ON e.businessentityid = ep.businessentityid
                    JOIN person.person p ON e.businessentityid = p.businessentityid
                    JOIN humanresources.employeedepartmenthistory edh ON e.businessentityid = edh.businessentityid
                    JOIN humanresources.department d ON edh.departmentid = d.departmentid
                    ")
                .ToList();
        }

        [Benchmark]
        public List<EmployeeWithPayDto> RepoDb_ORM()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<EmployeeWithPayDto>(
                @"
                SELECT e.businessentityid,
                       p.firstname,
                       p.lastname,
                       e.jobtitle,
                       d.name AS department,
                       ep.rate
                FROM humanresources.employee e
                JOIN humanresources.employeepayhistory ep ON e.businessentityid = ep.businessentityid
                JOIN person.person p ON e.businessentityid = p.businessentityid
                JOIN humanresources.employeedepartmenthistory edh ON e.businessentityid = edh.businessentityid
                JOIN humanresources.department d ON edh.departmentid = d.departmentid
                ")
                .ToList();
        }
        [Benchmark]
        public List<EmployeeWithPayDto> SqlSugar_ORM()
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
                SELECT e.businessentityid, p.firstname, p.lastname, e.jobtitle, d.name AS department, ep.rate
                FROM humanresources.employee e
                JOIN humanresources.employeepayhistory ep ON e.businessentityid = ep.businessentityid
                JOIN person.person p ON e.businessentityid = p.businessentityid
                JOIN humanresources.employeedepartmenthistory edh ON e.businessentityid = edh.businessentityid
                JOIN humanresources.department d ON edh.departmentid = d.departmentid";
            return _sqlSugarClient.Ado.SqlQuery<EmployeeWithPayDto>(sql);
        }
        [Benchmark]
        public List<EmployeeWithPayDto> OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();

            var sql = @"
                SELECT e.businessentityid,
                       p.firstname,
                       p.lastname,
                       e.jobtitle,
                       d.name AS department,
                       ep.rate
                FROM humanresources.employee e
                JOIN humanresources.employeepayhistory ep ON e.businessentityid = ep.businessentityid
                JOIN person.person p ON e.businessentityid = p.businessentityid
                JOIN humanresources.employeedepartmenthistory edh ON e.businessentityid = edh.businessentityid
                JOIN humanresources.department d ON edh.departmentid = d.departmentid";

            return db.SqlList<EmployeeWithPayDto>(sql);
        }

        [Benchmark]
        public List<EmployeeWithPayDto> FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);
            return _freeSqlPostgres.Ado.Query<EmployeeWithPayDto>(@"
                SELECT e.businessentityid, p.firstname, p.lastname, e.jobtitle, d.name AS department, ep.rate
                FROM humanresources.employee e
                JOIN humanresources.employeepayhistory ep ON e.businessentityid = ep.businessentityid
                JOIN person.person p ON e.businessentityid = p.businessentityid
                JOIN humanresources.employeedepartmenthistory edh ON e.businessentityid = edh.businessentityid
                JOIN humanresources.department d ON edh.departmentid = d.departmentid
            ").ToList();
        }
        [Benchmark]
        public List<EmployeeWithPayDto> EFCore_ORM()
        {
            using var context = CreatePostgresContext();

            return (from e in context.Employees
                    join ep in context.EmployeePayHistories on e.BusinessEntityId equals ep.BusinessEntityId
                    join p in context.People on e.BusinessEntityId equals p.BusinessEntityId
                    join edh in context.EmployeeDepartmentHistories on e.BusinessEntityId equals edh.BusinessEntityId
                    join d in context.Departments on edh.DepartmentId equals d.DepartmentId
                    select new EmployeeWithPayDto
                    {
                        BusinessEntityId = e.BusinessEntityId,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        JobTitle = e.JobTitle,
                        Department = d.Name,
                        Rate = ep.Rate
                    }).ToList();
        }
    }
}
