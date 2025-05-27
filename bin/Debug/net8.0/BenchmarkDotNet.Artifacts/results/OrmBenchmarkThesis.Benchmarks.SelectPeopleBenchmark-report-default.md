
BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5737/22H2/2022Update)
Intel Core i5-8400 CPU 2.80GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores
.NET SDK 9.0.203
  [Host] : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2 DEBUG

Job=MediumRun  Toolchain=InProcessNoEmitToolchain  InvocationCount=10  
IterationCount=10  LaunchCount=1  UnrollFactor=10  
WarmupCount=5  

 Method          | Mean     | Error    | StdDev   | Gen0      | Gen1      | Gen2     | Allocated |
---------------- |---------:|---------:|---------:|----------:|----------:|---------:|----------:|
 RepoDb_Postgres | 86.24 ms | 2.314 ms | 1.531 ms | 3100.0000 | 2000.0000 | 500.0000 |  16.52 MB |
