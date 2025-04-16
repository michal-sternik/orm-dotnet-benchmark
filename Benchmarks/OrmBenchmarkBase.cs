using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrmBenchmarkMag.Data;
using OrmBenchmarkMag.Models;
using RepoDb;


namespace OrmBenchmarkMag.Benchmarks
{
    public abstract class OrmBenchmarkBase
    {
        protected DbContextOptions<MssqlDbContext> _mssqlOptions;
        protected DbContextOptions<PostgresqlDbContext> _postgresOptions;

        protected string MssqlConnectionString { get; }
        protected string PostgresConnectionString { get; }

        protected OrmBenchmarkBase()
        {
            MssqlConnectionString = "Server=localhost,1433;Database=AdventureWorks2014;User Id=sa;Password=YourStr0ngP@ssw0rd!;TrustServerCertificate=True";
            PostgresConnectionString = "Host=localhost;Port=5432;Database=Adventureworks;Username=postgres;Password=postgres12";

            GlobalConfiguration.Setup().UseSqlServer();
            GlobalConfiguration.Setup().UsePostgreSql();

            _mssqlOptions = new DbContextOptionsBuilder<MssqlDbContext>()
                .UseSqlServer(MssqlConnectionString)
            .Options;

            _postgresOptions = new DbContextOptionsBuilder<PostgresqlDbContext>()
                .UseNpgsql(PostgresConnectionString)
                .Options;
        }

        //EfCore
        protected MssqlDbContext CreateMssqlContext() => new MssqlDbContext(_mssqlOptions);
        protected PostgresqlDbContext CreatePostgresContext() => new PostgresqlDbContext(_postgresOptions);

        //Dapper & RepoDB
        protected SqlConnection CreateMssqlConnection() => new SqlConnection(MssqlConnectionString);
        protected NpgsqlConnection CreatePostgresConnection() => new NpgsqlConnection(PostgresConnectionString);
    }
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
