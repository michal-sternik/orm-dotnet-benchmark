using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Config;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServiceStack.OrmLite;
using OrmBenchmarkMag.Benchmarks;
using SqlSugar;
using FreeSql;
using BenchmarkDotNet.Order;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectEmployeesWithPayBenchmarkMssql : OrmBenchmarkBase
    {
        [Params("Microsoft SQL Server")]
        public string DatabaseEngine { get; set; }
        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(RepoDb_ORM))]
        public void SetupRepoDbMssql()
        {
            FluentMapper.Entity<Employee>().Table("HumanResources.Employee");
            FluentMapper.Entity<EmployeePayHistory>().Table("HumanResources.EmployeePayHistory");
            FluentMapper.Entity<Person>().Table("Person.Person");
            FluentMapper.Entity<EmployeeDepartmentHistory>().Table("HumanResources.EmployeeDepartmentHistory");
            FluentMapper.Entity<Department>().Table("HumanResources.Department");
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
            SqlSugarSchemaConfigurator.ConfigureMappingsMssql(_sqlSugarClient);
        }

        private IFreeSql _freeSql;

        [GlobalSetup(Target = nameof(FreeSql_ORM))]
        public void SetupFreeSql()
        {
            _freeSql = new FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }




        [Benchmark]
        public List<EmployeeWithPayDto> Dapper_ORM()
        {
            using var connection = CreateMssqlConnection();
            return connection.Query<EmployeeWithPayDto>(
                @"SELECT e.BusinessEntityID, p.FirstName, p.LastName, e.JobTitle, d.Name AS Department, ep.Rate
                  FROM HumanResources.Employee e
                  JOIN HumanResources.EmployeePayHistory ep ON e.BusinessEntityID = ep.BusinessEntityID
                  JOIN Person.Person p ON e.BusinessEntityID = p.BusinessEntityID
                  JOIN HumanResources.EmployeeDepartmentHistory edh ON e.BusinessEntityID = edh.BusinessEntityID
                  JOIN HumanResources.Department d ON edh.DepartmentID = d.DepartmentID")
                .ToList();
        }

        [Benchmark]
        public List<EmployeeWithPayDto> RepoDb_ORM()
        {
            using var connection = CreateMssqlConnection();
            return connection.ExecuteQuery<EmployeeWithPayDto>(
                @"SELECT e.BusinessEntityID, p.FirstName, p.LastName, e.JobTitle, d.Name AS Department, ep.Rate
                  FROM HumanResources.Employee e
                  JOIN HumanResources.EmployeePayHistory ep ON e.BusinessEntityID = ep.BusinessEntityID
                  JOIN Person.Person p ON e.BusinessEntityID = p.BusinessEntityID
                  JOIN HumanResources.EmployeeDepartmentHistory edh ON e.BusinessEntityID = edh.BusinessEntityID
                  JOIN HumanResources.Department d ON edh.DepartmentID = d.DepartmentID")
                .ToList();
        }

        [Benchmark]
        public List<EmployeeWithPayDto> SqlSugar_ORM()
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
                SELECT e.BusinessEntityID, p.FirstName, p.LastName, e.JobTitle, d.Name AS Department, ep.Rate
                FROM HumanResources.Employee e
                JOIN HumanResources.EmployeePayHistory ep ON e.BusinessEntityID = ep.BusinessEntityID
                JOIN Person.Person p ON e.BusinessEntityID = p.BusinessEntityID
                JOIN HumanResources.EmployeeDepartmentHistory edh ON e.BusinessEntityID = edh.BusinessEntityID
                JOIN HumanResources.Department d ON edh.DepartmentID = d.DepartmentID";
            return _sqlSugarClient.Ado.SqlQuery<EmployeeWithPayDto>(sql);
        }


        [Benchmark]
        public List<EmployeeWithPayDto> OrmLite_ORM()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var sql = @"
                SELECT e.BusinessEntityID, p.FirstName, p.LastName, e.JobTitle, d.Name AS Department, ep.Rate
                FROM HumanResources.Employee e
                JOIN HumanResources.EmployeePayHistory ep ON e.BusinessEntityID = ep.BusinessEntityID
                JOIN Person.Person p ON e.BusinessEntityID = p.BusinessEntityID
                JOIN HumanResources.EmployeeDepartmentHistory edh ON e.BusinessEntityID = edh.BusinessEntityID
                JOIN HumanResources.Department d ON edh.DepartmentID = d.DepartmentID";

            return db.SqlList<EmployeeWithPayDto>(sql);
        }
        [Benchmark]
        public List<EmployeeWithPayDto> FreeSql_ORM()
        {
            _freeSql = new FreeSql.FreeSqlBuilder()
             .UseConnectionString(FreeSql.DataType.SqlServer, MssqlConnectionString)
             .UseAutoSyncStructure(false)
             .Build();
            return _freeSql.Ado.Query<EmployeeWithPayDto>(@"
                SELECT e.BusinessEntityID, p.FirstName, p.LastName, e.JobTitle, d.Name AS Department, ep.Rate
                FROM HumanResources.Employee e
                JOIN HumanResources.EmployeePayHistory ep ON e.BusinessEntityID = ep.BusinessEntityID
                JOIN Person.Person p ON e.BusinessEntityID = p.BusinessEntityID
                JOIN HumanResources.EmployeeDepartmentHistory edh ON e.BusinessEntityID = edh.BusinessEntityID
                JOIN HumanResources.Department d ON edh.DepartmentID = d.DepartmentID")
                .ToList();
        }

        [Benchmark]
        public List<EmployeeWithPayDto> EFCore_ORM()
        {
            using var context = CreateMssqlContext();

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

    public class EmployeeWithPayDto
    {
        public int BusinessEntityId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public decimal Rate { get; set; }
    }
}
