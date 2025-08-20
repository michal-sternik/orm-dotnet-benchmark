using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using SqlSugar;
using LinqToDB.Mapping;
namespace OrmBenchmarkMag.Models;

/// <summary>
/// Lookup table containing the departments within the Adventure Works Cycles company.
/// </summary>
[Table(Schema = "HumanResources", Name = "Departament")]
[Schema("HumanResources")]
[Table(Name = "HumanResources.Department")]
//[SugarTable("HumanResources.Department")]
public partial class Department
{
    /// <summary>
    /// Primary key for Department records.
    /// </summary>
    public short DepartmentId { get; set; }

    /// <summary>
    /// Name of the department.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Name of the group to which the department belongs.
    /// </summary>
    public string GroupName { get; set; } = null!;

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<EmployeeDepartmentHistory> EmployeeDepartmentHistories { get; set; } = new List<EmployeeDepartmentHistory>();
}
