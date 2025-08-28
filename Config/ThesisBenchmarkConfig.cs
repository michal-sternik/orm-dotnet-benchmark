// Config/ThesisBenchmarkConfig.cs
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Exporters;
using Microsoft.Extensions.Options;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;

namespace OrmBenchmarkMag.Config
{
    public class ThesisBenchmarkConfig : ManualConfig
    {

        public ThesisBenchmarkConfig()
        {
            this.HideColumns(Column.Gen0, Column.Gen1, Column.Gen2);
            //i to mi zwraca benchmark dla pojedynczego jednego wywolania danej metody "invoke" wiec to co chce
            //bedzie trzezba manipulować poniższymi wartościami dla bardziej wymagających queries, żeby nie czekać na wynik godzinami
            AddJob(Job.MediumRun
                .WithLaunchCount(1) 
                .WithWarmupCount(5)
                //standard - 10 ale dla insertow mamy 1 invocation wiec tutaj musi byc duzo
                .WithIterationCount(10)
                //dla krotko trwajacych queries - zwracajacych max 1000 rekordow
                //.WithInvocationCount(1000)
                //dla bardziej wymagajacych queries, ponad 1k, do 120k
                //dla insertow tutaj musi byc 1, bo czyscimy tabelę przed kazdym iteration (nie da sie przed invocation)
                .WithInvocationCount(50) 
                //unrollfactor musi dzielic invocationcount
                .WithUnrollFactor(10)
                .WithToolchain(InProcessNoEmitToolchain.Instance));

            AddDiagnoser(MemoryDiagnoser.Default);


            //time in ms
            SummaryStyle = BenchmarkDotNet.Reports.SummaryStyle.Default.WithTimeUnit(Perfolizer.Horology.TimeUnit.Millisecond);

            AddExporter(CsvExporter.Default);
            //WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));


            Options |= ConfigOptions.DisableOptimizationsValidator;
        }
    }
}