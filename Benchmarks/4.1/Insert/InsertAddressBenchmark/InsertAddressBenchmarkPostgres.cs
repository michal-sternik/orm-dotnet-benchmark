using BenchmarkDotNet.Attributes;
using Dapper;
using RepoDb;
using Microsoft.EntityFrameworkCore;
using OrmBenchmarkMag.Config;
using OrmBenchmarkMag.Models;
using ServiceStack.OrmLite;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using OrmBenchmarkMag.Benchmarks;

namespace OrmBenchmarkThesis.Benchmarks
{
    [Config(typeof(ThesisBenchmarkConfig))]
    [MemoryDiagnoser]
    public class InsertAddressBenchmarkPostgres : OrmBenchmarkBase
    {
        [Params("PostgreSQL")]
        public string DatabaseEngine { get; set; }
        private List<Address> _addresses;
        private SqlSugarClient _sqlSugarClient;
        private IFreeSql _freeSqlPostgres;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using var conn = CreatePostgresConnection();

            try
            {
                RepoDbSchemaConfigurator.Init();
            }
            catch (RepoDb.Exceptions.MappingExistsException)
            {
                // Already mapped
            }

            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
            OrmLiteSchemaConfigurator.ConfigureMappings();

            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();

            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);

            var origAddresses = conn.Query<Address>(
                @"SELECT * FROM person.address LIMIT 1"
            ).ToList();

            _addresses = new List<Address>(origAddresses.Count);

            foreach (var addr in origAddresses)
            {
                _addresses.Add(new Address
                {
                    AddressId = 0,
                    AddressLine1 = addr.AddressLine1,
                    AddressLine2 = addr.AddressLine2,
                    City = addr.City,
                    StateProvinceId = addr.StateProvinceId,
                    PostalCode = addr.PostalCode,
                    Rowguid = addr.Rowguid,
                    ModifiedDate = addr.ModifiedDate
                });
            }
        }
        //SELECT setval('person.address_addressid_seq', (SELECT COALESCE(MAX(addressid), 0) FROM person.address));
        //do zresetowania sekwencji na ostatni indeks
        [IterationSetup]
        public void IterationSetup()
        {
            using var conn = CreatePostgresConnection();
            var rand = new Random();

            var sequenceQuery = $@"SELECT nextval(pg_get_serial_sequence('person.address', 'addressid')) 
                                   FROM generate_series(1, {_addresses.Count})";

            var newIds = conn.Query<long>(sequenceQuery).ToList();

            for (int i = 0; i < _addresses.Count; i++)
            {
                _addresses[i].AddressLine1 = "Test Street " + rand.NextInt64(1_000_000_000_000_000, 9_999_999_999_999_999).ToString();
                _addresses[i].PostalCode = rand.Next(10000, 99999).ToString();
                _addresses[i].Rowguid = Guid.NewGuid();
                _addresses[i].AddressId = (int)newIds[i];
            }
        }

        [IterationCleanup]
        public void CleanupInserted()
        {
            using var conn = CreatePostgresConnection();
            var streets = _addresses.Select(x => x.AddressLine1).ToList();
            conn.Execute(@"DELETE FROM person.address WHERE addressline1 = ANY(@Streets)", new { Streets = streets });
        }


        //dapper insert - spatiallocation pomijamy (NULL)
        [Benchmark]
        public void Dapper_ORM()
        {
            using var conn = CreatePostgresConnection();

            conn.Execute(
                @"INSERT INTO person.address (addressid, addressline1, addressline2, city, stateprovinceid, postalcode, spatiallocation, rowguid, modifieddate)
                  VALUES (@AddressId, @AddressLine1, @AddressLine2, @City, @StateProvinceId, @PostalCode, NULL, @Rowguid, @ModifiedDate)", _addresses);
        }
        [Benchmark]
        public void RepoDb_ORM()
        {
            using var connection = CreatePostgresConnection();
            RepoDb.DbConnectionExtension.InsertAll(connection, _addresses);
        }


        [Benchmark]
        public void SqlSugar_ORM()
        {
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = PostgresConnectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            SqlSugarSchemaConfigurator.ConfigureMappingsPostgres(_sqlSugarClient);
            _sqlSugarClient.Insertable(_addresses).ExecuteCommand();
        }



        [Benchmark]
        public void OrmLite_ORM()
        {
            using var db = CreateOrmLitePostgresConnection();
            db.InsertAll(_addresses);
        }
        [Benchmark]
        public void FreeSql_ORM()
        {
            _freeSqlPostgres = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, PostgresConnectionString)
                .UseAutoSyncStructure(false)
                .Build();


            FreeSqlSchemaConfigurator.ConfigureMappingsPostgres(_freeSqlPostgres);

            _freeSqlPostgres.Insert<Address>()
                .AsTable("person.address")
                .AppendData(_addresses)
                .ExecuteAffrows();
        }
        [Benchmark]
        public void EFCore_ORM()
        {
            using var ctx = CreatePostgresContext();
            ctx.Addresses.AddRange(_addresses);
            ctx.SaveChanges();
        }
    }
}
