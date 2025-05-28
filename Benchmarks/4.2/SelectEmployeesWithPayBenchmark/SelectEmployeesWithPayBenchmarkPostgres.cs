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
using ServiceStack.DataAnnotations;
using ServiceStack;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectEmployeesWithPayBenchmarkPostgres : OrmBenchmarkBase
    {
        [GlobalSetup(Target = nameof(RepoDb_Postgres))]
        public void SetupRepoDbPostgres()
        {
            PostgresRepoDbMappingSetup.Init();
        }

        [GlobalSetup(Target = nameof(OrmLite_Postgres_LinqStyle))]
        public void SetupOrmLiteMappings()
        {
            OrmLiteSchemaConfigurator.ConfigureMappings();
        }

        [Benchmark]
        public List<EmployeeWithPayDto> EFCore_Postgres()
        {
            using var context = CreatePostgresContext();

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
        public List<EmployeeWithPayDto> Dapper_Postgres()
        {
            using var connection = CreatePostgresConnection();
            //return connection.Query<SalesOrderDetail>("SELECT * FROM Sales.SalesOrderDetail").ToList();
            return connection.Query<EmployeeWithPayDto>(
                @"
                    SELECT e.BusinessEntityID,
                           p.FirstName,
                           p.LastName,
                           e.JobTitle,
                           d.Name AS Department,
                           ep.Rate
                    FROM humanresources.employee e
                    JOIN HumanResources.EmployeePayHistory ep ON e.BusinessEntityID = ep.BusinessEntityID
                    JOIN Person.Person p ON e.BusinessEntityID = p.BusinessEntityID
                    JOIN HumanResources.EmployeeDepartmentHistory edh ON e.BusinessEntityID = edh.BusinessEntityID
                    JOIN HumanResources.Department d ON edh.DepartmentID = d.DepartmentID
                    ")
                .ToList();
        }

        [Benchmark]
        public List<EmployeeWithPayDto> RepoDb_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<EmployeeWithPayDto>(
                @"
                SELECT e.BusinessEntityID,
                       p.FirstName,
                       p.LastName,
                       e.JobTitle,
                       d.Name AS Department,
                       ep.Rate
                FROM HumanResources.Employee e
                JOIN HumanResources.EmployeePayHistory ep ON e.BusinessEntityID = ep.BusinessEntityID
                JOIN Person.Person p ON e.BusinessEntityID = p.BusinessEntityID
                JOIN HumanResources.EmployeeDepartmentHistory edh ON e.BusinessEntityID = edh.BusinessEntityID
                JOIN HumanResources.Department d ON edh.DepartmentID = d.DepartmentID
                ")
                .ToList();
        }

        [Benchmark]
        public List<EmployeeWithPayDto> OrmLite_Postgres_LinqStyle()
        {

            using var db = CreateOrmLitePostgresConnection();

            var q = db.From<Employee>()
                .Join<Employee, EmployeePayHistory>((e, ep) => e.BusinessEntityId == ep.BusinessEntityId)
                .Join<Employee, Person>((e, p) => e.BusinessEntityId == p.BusinessEntityId)
                .Join<Employee, EmployeeDepartmentHistory>((e, edh) => e.BusinessEntityId == edh.BusinessEntityId)
                .Join<EmployeeDepartmentHistory, Department>((edh, d) => edh.DepartmentId == d.DepartmentId)
                .Select(@"
            humanresources.employee.businessentityid,
            person.person.firstname,
            person.person.lastname,
            humanresources.employee.jobtitle,
            humanresources.department.name as department,
            humanresources.employeepayhistory.rate
        ");

            return db.Select<EmployeeWithPayDto>(q);
        }


    }


}
