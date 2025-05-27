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

        // StateProvince
        typeof(StateProvince).AddAttributes(new SchemaAttribute("person"));
        typeof(StateProvince)
            .GetProperty(nameof(StateProvince.StateProvinceId))
            .AddAttributes(new AliasAttribute("stateprovinceid"));
        typeof(StateProvince)
            .GetProperty(nameof(StateProvince.Name))
            .AddAttributes(new AliasAttribute("name"));



    

    }
}
