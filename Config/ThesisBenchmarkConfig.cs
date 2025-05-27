// Config/ThesisBenchmarkConfig.cs
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Exporters;
using Microsoft.Extensions.Options;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Columns;

namespace OrmBenchmarkMag.Config
{
    public class ThesisBenchmarkConfig : ManualConfig
    {
        //i ważna uwaga - do pracy nawet - Raw sql w ORMach używamy w ostatecznosci. my testujemy tutaj realne produkcyjne użycie, wiec testujemy wbudowane domyslne funkcjonalnosci
        //ormów tak jakby ktos pisal realna aplikacje. Nie ma sensu inaczej - bo wtedy dla kazdego ORMA byśmy stosowali Raw sql ktory jest najszybszy. a tak to tylko dapper
        //bedzie uzywal Raw sql bo wlasnie tak on dziala domyslnie
        public ThesisBenchmarkConfig()
        {
            this.HideColumns(Column.Gen0, Column.Gen1, Column.Gen2);
            //i to mi zwraca benchmark dla pojedynczego jednego wywolania danej metody "invoke" wiec to co chce
            //bedzie trzezba manipulować poniższymi wartościami dla bardziej wymagających queries, żeby nie czekać na wynik godzinami
            AddJob(Job.MediumRun
                .WithLaunchCount(1) 
                .WithWarmupCount(5)
                .WithIterationCount(10)
                //dla krotko trwajacych queries - zwracajacych max 1000 rekordow
                //.WithInvocationCount(1000)
                //dla bardziej wymagajacych queries, ponad 1k, do 120k
                .WithInvocationCount(1) 
                //unrollfactor musi dzielic invocationcount
                .WithUnrollFactor(1)
                .WithToolchain(InProcessNoEmitToolchain.Instance));

            AddDiagnoser(MemoryDiagnoser.Default);

            AddExporter(CsvExporter.Default);

            Options |= ConfigOptions.DisableOptimizationsValidator;
        }
    }
}