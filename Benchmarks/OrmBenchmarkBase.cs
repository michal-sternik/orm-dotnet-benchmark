using System.Data;
using LinqToDB.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrmBenchmarkMag.Data;
using OrmBenchmarkMag.Models;
using RepoDb;
using ServiceStack.OrmLite;


namespace OrmBenchmarkMag.Benchmarks
{
    public abstract class OrmBenchmarkBase
    {
        protected DbContextOptions<MssqlDbContext> _mssqlOptions;
        protected DbContextOptions<PostgresqlDbContext> _postgresOptions;

        protected string MssqlConnectionString { get; }
        protected string PostgresConnectionString { get; }

        private OrmLiteConnectionFactory _mssqlOrmLiteFactory;
        private OrmLiteConnectionFactory _postgresOrmLiteFactory;

        protected OrmBenchmarkBase()
        {
            MssqlConnectionString = "Server=localhost,1433;Database=AdventureWorks2014;User Id=sa;Password=YourStr0ngP@ssw0rd!;TrustServerCertificate=True";
            PostgresConnectionString = "Host=localhost;Port=5432;Database=Adventureworks;Username=postgres;Password=postgres12";

            GlobalConfiguration.Setup().UseSqlServer();
            GlobalConfiguration.Setup().UsePostgreSql();

            //OrmLiteConfig.DialectProvider = PostgreSqlDialect.Provider;
            //OrmLiteSchemaConfigurator.ConfigureSchemaAttributes();

            _mssqlOptions = new DbContextOptionsBuilder<MssqlDbContext>()
                .UseSqlServer(MssqlConnectionString)
            .Options;

            _postgresOptions = new DbContextOptionsBuilder<PostgresqlDbContext>()
                .UseNpgsql(PostgresConnectionString)
                .Options;


            //ormlite factiories
            _mssqlOrmLiteFactory = new OrmLiteConnectionFactory(MssqlConnectionString, SqlServerDialect.Provider);

            OrmLiteConfig.DialectProvider = PostgreSqlDialect.Provider;
            OrmLiteConfig.DialectProvider.NamingStrategy = new PostgreSqlNamingStrategy();

            // Potem utwórz connection factory
            _postgresOrmLiteFactory = new OrmLiteConnectionFactory(PostgresConnectionString, PostgreSqlDialect.Provider);
            



        }

        //EfCore
        protected MssqlDbContext CreateMssqlContext() => new MssqlDbContext(_mssqlOptions);
        protected PostgresqlDbContext CreatePostgresContext() => new PostgresqlDbContext(_postgresOptions);

        //Dapper & RepoDB
        protected SqlConnection CreateMssqlConnection() => new SqlConnection(MssqlConnectionString);
        protected NpgsqlConnection CreatePostgresConnection() => new NpgsqlConnection(PostgresConnectionString);

        //Linq2db
        //protected DataConnection CreateLinq2DbMssqlConnection()
        //{
        //    var dc = new DataConnection(LinqToDB.ProviderName.SqlServer, MssqlConnectionString);
        //    dc.MappingSchema.DefaultIgnoreEmptyTypes = false; // <-- to mówi: mapuj wszystkie klasy nawet bez [Table]
        //    return dc;
        //}
        //protected DataConnection CreateLinq2DbPostgresConnection() => new DataConnection(LinqToDB.ProviderName.PostgreSQL, PostgresConnectionString);

        // OrmLite
        protected IDbConnection CreateOrmLiteMssqlConnection() => _mssqlOrmLiteFactory.OpenDbConnection();
        protected IDbConnection CreateOrmLitePostgresConnection() => _postgresOrmLiteFactory.OpenDbConnection();

    }
}


public class PostgreSqlNamingStrategy : OrmLiteNamingStrategyBase
{
    public override string GetTableName(string name) => name.ToLower();
    public override string GetSchemaName(string name) => name?.ToLower();

}

//using BenchmarkDotNet.Attributes;
//using Microsoft.EntityFrameworkCore;
//using OrmBenchmarkMag.Data;


//namespace OrmBenchmarkMag.Benchmarks
//{
//    public abstract class OrmBenchmarkBase
//    {
//        protected const string MssqlConnection = "Server=localhost,1433;Database=AdventureWorks2014;User Id=sa;Password=YourStr0ngP@ssw0rd!;TrustServerCertificate=True";
//        protected const string PostgresConnection = "Host=localhost;Port=5432;Database=Adventureworks;Username=postgres;Password=postgres12";

//        protected MssqlDbContext? _mssqlContext;
//        protected PostgresqlDbContext? _postgresContext;

//        [GlobalSetup]
//        public void Setup()
//        {
//            var mssqlOptions = new DbContextOptionsBuilder<MssqlDbContext>()
//                .UseSqlServer(MssqlConnection).Options;
//            _mssqlContext = new MssqlDbContext(mssqlOptions);

//            var postgresOptions = new DbContextOptionsBuilder<PostgresqlDbContext>()
//                .UseNpgsql(PostgresConnection).Options;
//            _postgresContext = new PostgresqlDbContext(postgresOptions);
//        }


//    }
//}
