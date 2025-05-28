using OrmBenchmarkMag.Models;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.PostgreSQL;
using ServiceStack.OrmLite.SqlServer;

public static class OrmLiteSchemaConfigurator
{
    public static void ConfigureMappings()
    {
        typeof(Employee).AddAttributes(new SchemaAttribute("humanresources"));
        typeof(EmployeePayHistory).AddAttributes(new SchemaAttribute("humanresources"));
        typeof(EmployeeDepartmentHistory).AddAttributes(new SchemaAttribute("humanresources"));
        typeof(Department).AddAttributes(new SchemaAttribute("humanresources"));
        typeof(Person).AddAttributes(new SchemaAttribute("person"));
        typeof(Customer).AddAttributes(new SchemaAttribute("sales"));
        typeof(Product).AddAttributes(new SchemaAttribute("production"));
        typeof(ProductCategory).AddAttributes(new SchemaAttribute("production"));
        typeof(ProductSubcategory).AddAttributes(new SchemaAttribute("production"));
        typeof(UnitMeasure).AddAttributes(new SchemaAttribute("production"));
        typeof(SalesOrderDetail).AddAttributes(new SchemaAttribute("sales"));

        typeof(Employee)
            .GetProperty(nameof(Employee.BusinessEntityId))
            .AddAttributes(new AliasAttribute("businessentityid"));
        typeof(EmployeePayHistory)
            .GetProperty(nameof(EmployeePayHistory.BusinessEntityId))
            .AddAttributes(new AliasAttribute("businessentityid"));
        typeof(EmployeeDepartmentHistory)
            .GetProperty(nameof(EmployeeDepartmentHistory.BusinessEntityId))
            .AddAttributes(new AliasAttribute("businessentityid"));
        typeof(EmployeeDepartmentHistory)
            .GetProperty(nameof(EmployeeDepartmentHistory.DepartmentId))
            .AddAttributes(new AliasAttribute("departmentid"));
        typeof(Department)
            .GetProperty(nameof(Department.DepartmentId))
            .AddAttributes(new AliasAttribute("departmentid"));
        typeof(Department)
            .GetProperty(nameof(Department.Name))
            .AddAttributes(new AliasAttribute("name"));
        typeof(Person)
            .GetProperty(nameof(Person.BusinessEntityId))
            .AddAttributes(new AliasAttribute("businessentityid"));
        typeof(Person)
            .GetProperty(nameof(Person.FirstName))
            .AddAttributes(new AliasAttribute("firstname"));
        typeof(Person)
            .GetProperty(nameof(Person.LastName))
            .AddAttributes(new AliasAttribute("lastname"));

        // Customer

        typeof(Customer)
            .GetProperty(nameof(Customer.CustomerId))
            .AddAttributes(new AliasAttribute("customerid"));
        typeof(Customer)
            .GetProperty(nameof(Customer.PersonId))
            .AddAttributes(new AliasAttribute("personid"));
        typeof(Customer)
            .GetProperty(nameof(Customer.AccountNumber))
            .AddAttributes(new AliasAttribute("accountnumber"));

        // SalesOrderHeader
        typeof(SalesOrderHeader).AddAttributes(new SchemaAttribute("sales"));
        typeof(SalesOrderHeader)
            .GetProperty(nameof(SalesOrderHeader.SalesOrderId))
            .AddAttributes(new AliasAttribute("salesorderid"));
        typeof(SalesOrderHeader)
            .GetProperty(nameof(SalesOrderHeader.CustomerId))
            .AddAttributes(new AliasAttribute("customerid"));
        typeof(SalesOrderHeader)
            .GetProperty(nameof(SalesOrderHeader.BillToAddressId))
            .AddAttributes(new AliasAttribute("billtoaddressid"));
        typeof(SalesOrderHeader)
            .GetProperty(nameof(SalesOrderHeader.ShipToAddressId))
            .AddAttributes(new AliasAttribute("shiptoaddressid"));

        // Address
        typeof(Address).AddAttributes(new SchemaAttribute("person"));
        typeof(Address)
            .GetProperty(nameof(Address.AddressId))
            .AddAttributes(new AliasAttribute("addressid"));
        typeof(Address)
            .GetProperty(nameof(Address.AddressLine1))
            .AddAttributes(new AliasAttribute("addressline1"));
        typeof(Address)
            .GetProperty(nameof(Address.StateProvinceId))
            .AddAttributes(new AliasAttribute("stateprovinceid"));
        typeof(Address)
            .GetProperty(nameof(Address.City))
            .AddAttributes(new AliasAttribute("city"));

        // StateProvince
        typeof(StateProvince).AddAttributes(new SchemaAttribute("person"));
        typeof(StateProvince)
            .GetProperty(nameof(StateProvince.StateProvinceId))
            .AddAttributes(new AliasAttribute("stateprovinceid"));
        typeof(StateProvince)
            .GetProperty(nameof(StateProvince.Name))
            .AddAttributes(new AliasAttribute("name"));


        // Production schema
        typeof(Product)
            .GetProperty(nameof(Product.Name))
            .AddAttributes(new AliasAttribute("name"));
        typeof(ProductCategory)
            .GetProperty(nameof(ProductCategory.Name))
            .AddAttributes(new AliasAttribute("name"));
        typeof(ProductSubcategory)
            .GetProperty(nameof(ProductSubcategory.Name))
            .AddAttributes(new AliasAttribute("name"));
        typeof(UnitMeasure)
            .GetProperty(nameof(UnitMeasure.Name))
            .AddAttributes(new AliasAttribute("name"));
        typeof(Product)
            .GetProperty(nameof(Product.ProductId))
            .AddAttributes(new AliasAttribute("productid"));

        typeof(UnitMeasure)
            .GetProperty(nameof(UnitMeasure.UnitMeasureCode))
            .AddAttributes(new AliasAttribute("unitmeasurecode"));

        typeof(Product)
            .GetProperty(nameof(Product.ProductSubcategoryId))
            .AddAttributes(new AliasAttribute("productsubcategoryid"));
        typeof(Product)
            .GetProperty(nameof(Product.WeightUnitMeasureCode))
            .AddAttributes(new AliasAttribute("weightunitmeasurecode"));
        typeof(Product)
            .GetProperty(nameof(Product.SizeUnitMeasureCode))
            .AddAttributes(new AliasAttribute("sizeunitmeasurecode"));

        typeof(ProductCategory)
            .GetProperty(nameof(ProductCategory.ProductCategoryId))
            .AddAttributes(new AliasAttribute("productcategoryid"));

        typeof(ProductSubcategory)
            .GetProperty(nameof(ProductSubcategory.ProductCategoryId))
            .AddAttributes(new AliasAttribute("productcategoryid"));
        typeof(ProductSubcategory)
            .GetProperty(nameof(ProductSubcategory.ProductSubcategoryId))
            .AddAttributes(new AliasAttribute("productsubcategoryid"));

        // SalesOrderDetail - pola
        typeof(SalesOrderDetail)
            .GetProperty(nameof(SalesOrderDetail.SalesOrderId))
            .AddAttributes(new AliasAttribute("salesorderid"));
        typeof(SalesOrderDetail)
            .GetProperty(nameof(SalesOrderDetail.ProductId))
            .AddAttributes(new AliasAttribute("productid"));
        typeof(SalesOrderDetail)
            .GetProperty(nameof(SalesOrderDetail.OrderQty))
            .AddAttributes(new AliasAttribute("orderqty"));



    }
}
