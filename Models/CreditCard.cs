using System;
using System.Collections.Generic;
using FreeSql.DataAnnotations;
using ServiceStack.DataAnnotations;
using SqlSugar;

namespace OrmBenchmarkMag.Models;

/// <summary>
/// Customer credit card information.
/// </summary>
[Alias("CreditCard")]
[Schema("Sales")]
[Table(Name = "Sales.CreditCard")]//freesql
public partial class CreditCard
{
    /// <summary>
    /// Primary key for CreditCard records.
    /// </summary>
    /// 
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    [AutoIncrement]
    [Column(IsIdentity = true)]//freesql
    public int CreditCardId { get; set; }

    /// <summary>
    /// Credit card name.
    /// </summary>
    public string CardType { get; set; } = null!;

    /// <summary>
    /// Credit card number.
    /// </summary>
    public string CardNumber { get; set; } = null!;

    /// <summary>
    /// Credit card expiration month.
    /// </summary>
    public byte ExpMonth { get; set; }

    /// <summary>
    /// Credit card expiration year.
    /// </summary>
    public short ExpYear { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }
    [SugarColumn(IsIgnore = true)]
    [Ignore]
    public virtual ICollection<PersonCreditCard> PersonCreditCards { get; set; } = new List<PersonCreditCard>();
    [SugarColumn(IsIgnore = true)]
    [Ignore]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeaders { get; set; } = new List<SalesOrderHeader>();
}
