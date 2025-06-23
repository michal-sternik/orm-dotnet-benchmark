using System;
using System.Collections.Generic;
using FreeSql.DataAnnotations;
using ServiceStack.DataAnnotations;
using SqlSugar;

namespace OrmBenchmarkMag.Models;

/// <summary>
/// Street address information for customers, employees, and vendors.
/// </summary>
[Alias("Address")]
[Schema("Person")]
[Table(Name = "Person.Address")] // FreeSql mapping
public partial class Address
{
    /// <summary>
    /// Primary key for Address records.
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    [AutoIncrement]
    [Column(IsIdentity = true)] // FreeSql
    public int AddressId { get; set; }

    /// <summary>
    /// First street address line.
    /// </summary>
    public string AddressLine1 { get; set; } = null!;

    /// <summary>
    /// Second street address line.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Name of the city.
    /// </summary>
    public string City { get; set; } = null!;

    /// <summary>
    /// Unique identification number for the state or province. Foreign key to StateProvince table.
    /// </summary>
    public int StateProvinceId { get; set; }

    /// <summary>
    /// Postal code for the street address.
    /// </summary>
    public string PostalCode { get; set; } = null!;

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid Rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    // Ignorowanie pól nawigacyjnych
    [SugarColumn(IsIgnore = true)]
    [Ignore]
    [Column(IsIgnore = true)]
    public virtual ICollection<BusinessEntityAddress> BusinessEntityAddresses { get; set; } = new List<BusinessEntityAddress>();

    [SugarColumn(IsIgnore = true)]
    [Ignore]
    [Column(IsIgnore = true)]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeaderBillToAddresses { get; set; } = new List<SalesOrderHeader>();

    [SugarColumn(IsIgnore = true)]
    [Ignore]
    [Column(IsIgnore = true)]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeaderShipToAddresses { get; set; } = new List<SalesOrderHeader>();

    [SugarColumn(IsIgnore = true)]
    [Ignore]
    [Column(IsIgnore = true)]
    public virtual StateProvince StateProvince { get; set; } = null!;
}
