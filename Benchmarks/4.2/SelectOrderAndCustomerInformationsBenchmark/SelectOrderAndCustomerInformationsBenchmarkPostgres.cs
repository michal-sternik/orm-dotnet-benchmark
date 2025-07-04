﻿using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using OrmBenchmarkMag.Models;
using OrmBenchmarkMag.Config;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServiceStack.OrmLite;
using OrmBenchmarkMag.Benchmarks;
using SqlSugar;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class SelectOrderAndCustomerInformationsBenchmarkPostgres : OrmBenchmarkBase
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

        private IFreeSql _freeSqlPostgres;

        [GlobalSetup(Target = nameof(FreeSql_Postgres))]
        public void SetupFreeSqlPostgres()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();
        }

        [Benchmark]
        public List<OrderProductDetailDto> FreeSql_Postgres()
        {
            return _freeSqlPostgres.Ado.Query<OrderProductDetailDto>(@"
                SELECT soh.salesorderid,
                        p.name AS productname,
                        sod.orderqty,
                        pe.firstname,
                        pe.lastname,
                        c.accountnumber,
                        a.city
                FROM sales.salesorderheader soh
                JOIN sales.salesorderdetail sod ON soh.salesorderid = sod.salesorderid
                JOIN production.product p ON sod.productid = p.productid
                JOIN sales.customer c ON soh.customerid = c.customerid
                LEFT JOIN person.person pe ON c.customerid = pe.businessentityid
                JOIN person.address a ON soh.shiptoaddressid = a.addressid
                ORDER BY soh.salesorderid
            ").ToList();
        }


        private SqlSugarClient _sqlSugarClient;

        [GlobalSetup(Target = nameof(SqlSugar_Postgres))]
        public void SetupSqlSugar()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
        }

        [Benchmark]
        public List<OrderProductDetailDto> SqlSugar_Postgres()
        {
            var list = _sqlSugarClient.Queryable<SalesOrderHeader>()
                .LeftJoin<SalesOrderDetail>((soh, sod) => soh.SalesOrderId == sod.SalesOrderId)
                .LeftJoin<Product>((soh, sod, p) => sod.ProductId == p.ProductId)
                .LeftJoin<Customer>((soh, sod, p, c) => soh.CustomerId == c.CustomerId)
                .LeftJoin<Person>((soh, sod, p, c, pe) => c.CustomerId == pe.BusinessEntityId)
                .LeftJoin<Address>((soh, sod, p, c, pe, a) => soh.ShipToAddressId == a.AddressId)
                .OrderBy((soh, sod, p, c, pe, a) => soh.SalesOrderId)
                .Select((soh, sod, p, c, pe, a) => new OrderProductDetailDto
                {
                    SalesOrderId = soh.SalesOrderId,
                    ProductName = p.Name,
                    OrderQty = sod.OrderQty,
                    FirstName = pe.FirstName,
                    LastName = pe.LastName,
                    AccountNumber = c.AccountNumber,
                    City = a.City
                })
                .ToList();

            return list;
        }


        [Benchmark]
        public List<OrderProductDetailDto> Dapper_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.Query<OrderProductDetailDto>(
                @"
                SELECT soh.salesorderid,
                       p.name AS productname,
                       sod.orderqty,
                       pe.firstname,
                       pe.lastname,
                       c.accountnumber,
                       a.city
                FROM sales.salesorderheader soh
                JOIN sales.salesorderdetail sod ON soh.salesorderid = sod.salesorderid
                JOIN production.product p ON sod.productid = p.productid
                JOIN sales.customer c ON soh.customerid = c.customerid
                LEFT JOIN person.person pe ON c.customerid = pe.businessentityid
                JOIN person.address a ON soh.shiptoaddressid = a.addressid
                ORDER BY soh.salesorderid")
                .ToList();
        }

        [Benchmark]
        public List<OrderProductDetailDto> RepoDb_Postgres()
        {
            using var connection = CreatePostgresConnection();
            return connection.ExecuteQuery<OrderProductDetailDto>(
                @"
                SELECT soh.salesorderid,
                       p.name AS productname,
                       sod.orderqty,
                       pe.firstname,
                       pe.lastname,
                       c.accountnumber,
                       a.city
                FROM sales.salesorderheader soh
                JOIN sales.salesorderdetail sod ON soh.salesorderid = sod.salesorderid
                JOIN production.product p ON sod.productid = p.productid
                JOIN sales.customer c ON soh.customerid = c.customerid
                LEFT JOIN person.person pe ON c.customerid = pe.businessentityid
                JOIN person.address a ON soh.shiptoaddressid = a.addressid
                ORDER BY soh.salesorderid")
                .ToList();
        }

        [Benchmark]
        public List<OrderProductDetailDto> OrmLite_Postgres_LinqStyle()
        {
            using var db = CreateOrmLitePostgresConnection();

            var q = db.From<SalesOrderHeader>()
                .Join<SalesOrderHeader, SalesOrderDetail>((soh, sod) => soh.SalesOrderId == sod.SalesOrderId)
                .Join<SalesOrderDetail, Product>((sod, p) => sod.ProductId == p.ProductId)
                .Join<SalesOrderHeader, Customer>((soh, c) => soh.CustomerId == c.CustomerId)
                .LeftJoin<Customer, Person>((c, pe) => c.CustomerId == pe.BusinessEntityId)
                .Join<SalesOrderHeader, Address>((soh, a) => soh.ShipToAddressId == a.AddressId)
                .Select(@"
                    sales.salesorderheader.salesorderid,
                    production.product.name AS productname,
                    sales.salesorderdetail.orderqty,
                    person.person.firstname,
                    person.person.lastname,
                    sales.customer.accountnumber,
                    person.address.city
                ");

            return db.Select<OrderProductDetailDto>(q);
        }


        [Benchmark]
        public List<OrderProductDetailDto> EFCore_Postgres_WithIncludes()
        {
            using var context = CreatePostgresContext();

            return context.SalesOrderHeaders
                .Include(soh => soh.Customer)
                .ThenInclude(c => c.Person)
                .Include(soh => soh.SalesOrderDetails)
                    .ThenInclude(sod => sod.Product)
                .Include(soh => soh.ShipToAddress)
                .SelectMany(soh => soh.SalesOrderDetails.Select(sod => new OrderProductDetailDto
                {
                    SalesOrderId = soh.SalesOrderId,
                    ProductName = sod.Product.Name,
                    OrderQty = sod.OrderQty,
                    FirstName = soh.Customer.Person.FirstName,
                    LastName = soh.Customer.Person.LastName,
                    AccountNumber = soh.Customer.AccountNumber,
                    City = soh.ShipToAddress.City
                }))
                .OrderBy(x => x.SalesOrderId)
                .ToList();
        }




    }

}
