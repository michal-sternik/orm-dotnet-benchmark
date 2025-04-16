using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using OrmBenchmarkMag.Models;
using RepoDb;

public static class RepoDbMappingSetup
{
    private static bool _initialized = false;
    private static readonly object _lock = new();

    public static void Init()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            // Inicjalizacja providerów
            //SqlServerBootstrap.Initialize();
            //PostgreSqlBootstrap.Initialize();

            // Mapowanie modeli
            MapEntityWithLowercase<SalesOrderHeader>("Sales.SalesOrderHeader", "sales.salesorderheader");
            // Możesz tu dodać kolejne:
            // MapEntityWithLowercase<Customer>("Sales.Customer", "sales.customer");
            // MapEntityWithLowercase<SalesOrderDetail>("Sales.SalesOrderDetail", "sales.salesorderdetail");

            _initialized = true;
        }
    }

    public static void MapEntityWithLowercase<T>(string mssqlTable, string postgresTable) where T : class
    {
        // MSSQL
        //FluentMapper
        //    .Entity<T>()
        //    .Table(mssqlTable);

        // Postgres
        var mapping = FluentMapper
            .Entity<T>()
            .Table(postgresTable);

        var type = typeof(T);
        var param = Expression.Parameter(type, "x");

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var colName = prop.Name.ToLower(CultureInfo.InvariantCulture);

            // budujemy lambda: x => x.Prop
            var propAccess = Expression.Property(param, prop);
            var converted = Expression.Convert(propAccess, typeof(object));
            var lambda = Expression.Lambda<Func<T, object>>(converted, param);

            // mapujemy kolumnę
            mapping.Column(lambda, colName);
        }
    }
}
