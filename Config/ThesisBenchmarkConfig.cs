// Config/ThesisBenchmarkConfig.cs
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Exporters;
using Microsoft.Extensions.Options;
using BenchmarkDotNet.Exporters.Csv;

namespace OrmBenchmarkMag.Config
{
    public class ThesisBenchmarkConfig : ManualConfig
    {
        public ThesisBenchmarkConfig()
        {
            //i to mi zwraca benchmark dla pojedynczego jednego wywolania danej metody "invoke" wiec to co chce
            //bedzie trzezba manipulować poniższymi wartościami dla bardziej wymagających queries, żeby nie czekać na wynik godzinami
            AddJob(Job.MediumRun
                .WithLaunchCount(1) 
                .WithWarmupCount(5)
                .WithIterationCount(10)
                //dla krotko trwajacych queries - zwracajacych max 1000 rekordow
                //.WithInvocationCount(1000)
                //dla bardziej wymagajacych queries, ponad 1k, do 120k
                .WithInvocationCount(10) 
                .WithUnrollFactor(10)
                .WithToolchain(InProcessNoEmitToolchain.Instance));

            AddDiagnoser(MemoryDiagnoser.Default);

            AddExporter(MarkdownExporter.Default);
            AddExporter(CsvMeasurementsExporter.Default);
            AddExporter(HtmlExporter.Default);

            Options |= ConfigOptions.DisableOptimizationsValidator;
        }
    }
}