using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Config;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServiceStack.OrmLite;
using LinqToDB;
using OrmBenchmarkMag.Benchmarks;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectEmployeesWithPayBenchmarkMssql : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_MSSQL))]
        public void SetupRepoDbMssql()
        {
            FluentMapper.Entity<Employee>().Table("HumanResources.Employee");
            FluentMapper.Entity<EmployeePayHistory>().Table("HumanResources.EmployeePayHistory");
            FluentMapper.Entity<Person>().Table("Person.Person");
            FluentMapper.Entity<EmployeeDepartmentHistory>().Table("HumanResources.EmployeeDepartmentHistory");
            FluentMapper.Entity<Department>().Table("HumanResources.Department");
        }

        [Benchmark]
        public List<EmployeeWithPayDto> OrmLite_MSSQL_LinqStyle()
        {
            using var db = CreateOrmLiteMssqlConnection();

            var q = db.From<Employee>()
                .Join<Employee, EmployeePayHistory>((e, ep) => e.BusinessEntityId == ep.BusinessEntityId)
                .Join<Employee, Person>((e, p) => e.BusinessEntityId == p.BusinessEntityId)
                .Join<Employee, EmployeeDepartmentHistory>((e, edh) => e.BusinessEntityId == edh.BusinessEntityId)
                .Join<EmployeeDepartmentHistory, Department>((edh, d) => edh.DepartmentId == d.DepartmentId)
                .Select("HumanResources.Employee.BusinessEntityID, Person.Person.FirstName, Person.Person.LastName, HumanResources.Employee.JobTitle, HumanResources.Department.Name as Department, HumanResources.EmployeePayHistory.Rate");


            return db.Select<EmployeeWithPayDto>(q);
        }

        [Benchmark]
        public List<EmployeeWithPayDto> OrmLite_MSSQL()
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
        public List<EmployeeWithPayDto> EFCore_MSSQL_WithJoins()
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

        [Benchmark]
        public List<EmployeeWithPayDto> EFCore_MSSQL_IncludeStyle()
        {
            using var context = CreateMssqlContext();

            return context.Employees
                .Include(e => e.BusinessEntity)
                .Include(e => e.EmployeePayHistories)
                .Include(e => e.EmployeeDepartmentHistories)
                    .ThenInclude(edh => edh.Department)
                .Select(e => new EmployeeWithPayDto
                {
                    BusinessEntityId = e.BusinessEntityId,
                    FirstName = e.BusinessEntity.FirstName,
                    LastName = e.BusinessEntity.LastName,
                    JobTitle = e.JobTitle,
                    Department = e.EmployeeDepartmentHistories.FirstOrDefault().Department.Name,
                    Rate = e.EmployeePayHistories.FirstOrDefault().Rate
                })
                .ToList();
        }

        [Benchmark]
        public List<EmployeeWithPayDto> Dapper_MSSQL()
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
        public List<EmployeeWithPayDto> RepoDb_MSSQL()
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
