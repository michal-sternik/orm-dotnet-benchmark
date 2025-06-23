using OrmBenchmarkMag.Models;
using SqlSugar;


public static class SqlSugarSchemaConfigurator
{
    public static void ConfigureMappingsPostgres(SqlSugarClient db)
    {

        db.MappingTables.Add(typeof(Employee).Name, "humanresources.employee");
        db.MappingTables.Add(typeof(EmployeePayHistory).Name, "humanresources.employeepayhistory");
        db.MappingTables.Add(typeof(EmployeeDepartmentHistory).Name, "humanresources.employeedepartmenthistory");
        db.MappingTables.Add(typeof(Department).Name, "humanresources.department");
        db.MappingTables.Add(typeof(Person).Name, "person.person");
        db.MappingTables.Add(typeof(Customer).Name, "sales.customer");
        db.MappingTables.Add(typeof(Product).Name, "production.product");
        db.MappingTables.Add(typeof(ProductCategory).Name, "production.productcategory");
        db.MappingTables.Add(typeof(ProductSubcategory).Name, "production.productsubcategory");
        db.MappingTables.Add(typeof(UnitMeasure).Name, "production.unitmeasure");
        db.MappingTables.Add(typeof(SalesOrderDetail).Name, "sales.salesorderdetail");
        db.MappingTables.Add(typeof(SalesOrderHeader).Name, "sales.salesorderheader");
        db.MappingTables.Add(typeof(Address).Name, "person.address");
        db.MappingTables.Add(typeof(StateProvince).Name, "person.stateprovince");
        db.MappingTables.Add(typeof(Shift).Name, "humanresources.shift");
        db.MappingTables.Add(typeof(CreditCard).Name, "sales.creditcard");
        db.MappingTables.Add(typeof(Address).Name, "person.address");
        //db.MappingTables.Add(typeof(PersonCreditCard).Name, "sales.personcreditcard");
    }

    public static void ConfigureMappingsMssql(SqlSugarClient db)
    {
        db.MappingTables.Add(typeof(Employee).Name, "HumanResources.Employee");
        db.MappingTables.Add(typeof(EmployeePayHistory).Name, "HumanResources.EmployeePayHistory");
        db.MappingTables.Add(typeof(EmployeeDepartmentHistory).Name, "HumanResources.EmployeeDepartmentHistory");
        db.MappingTables.Add(typeof(Department).Name, "HumanResources.Department");
        db.MappingTables.Add(typeof(Person).Name, "Person.Person");
        db.MappingTables.Add(typeof(Customer).Name, "Sales.Customer");
        db.MappingTables.Add(typeof(Product).Name, "Production.Product");
        db.MappingTables.Add(typeof(ProductCategory).Name, "Production.ProductCategory");
        db.MappingTables.Add(typeof(ProductSubcategory).Name, "Production.ProductSubcategory");
        db.MappingTables.Add(typeof(UnitMeasure).Name, "Production.UnitMeasure");
        db.MappingTables.Add(typeof(SalesOrderDetail).Name, "Sales.SalesOrderDetail");
        db.MappingTables.Add(typeof(SalesOrderHeader).Name, "Sales.SalesOrderHeader");
        db.MappingTables.Add(typeof(Address).Name, "Person.Address");
        db.MappingTables.Add(typeof(StateProvince).Name, "Person.StateProvince");
        db.MappingTables.Add(typeof(Shift).Name, "HumanResources.Shift");
        db.MappingTables.Add(typeof(CreditCard).Name, "Sales.CreditCard");
        db.MappingTables.Add(typeof(PersonCreditCard).Name, "Sales.PersonCreditCard");
        db.MappingTables.Add(typeof(Address).Name, "Person.Address");

        // MAPPING KOLUMN na lower-case (jeśli trzeba, np. bo masz property "BusinessEntityId", a w PG kolumna to "businessentityid")  
        //db.MappingColumns.Add("BusinessEntityId", typeof(Employee).Name, "businessentityid");
        //db.MappingColumns.Add("BusinessEntityId", typeof(EmployeePayHistory).Name, "businessentityid");

        // Powtarzaj powyższe dla wszystkich właściwości, które tego wymagają  
    }
}
