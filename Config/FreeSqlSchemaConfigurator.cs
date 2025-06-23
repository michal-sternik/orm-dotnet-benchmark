using OrmBenchmarkMag.Models;

public static class FreeSqlSchemaConfigurator
{
    public static void ConfigureMappingsPostgres(IFreeSql db)
    {
        //to dopiero dla insertow zostalo wprowadzone
        db.CodeFirst.ConfigEntity<CreditCard>(entity =>
        {
            entity.Property(a => a.CreditCardId).Name("creditcardid");
            entity.Property(a => a.CardType).Name("cardtype");
            entity.Property(a => a.CardNumber).Name("cardnumber");
            entity.Property(a => a.ExpMonth).Name("expmonth");
            entity.Property(a => a.ExpYear).Name("expyear");
            entity.Property(a => a.ModifiedDate).Name("modifieddate");

        });
        db.CodeFirst.ConfigEntity<Address>(entity =>
        {
            entity.Property(a => a.AddressId).Name("addressid");
            entity.Property(a => a.AddressLine1).Name("addressline1");
            entity.Property(a => a.AddressLine2).Name("addressline2");
            entity.Property(a => a.City).Name("city");
            entity.Property(a => a.StateProvinceId).Name("stateprovinceid");
            entity.Property(a => a.PostalCode).Name("postalcode");
            entity.Property(a => a.ModifiedDate).Name("modifieddate");
            entity.Property(a => a.Rowguid).Name("rowguid");

        });



    }
}
