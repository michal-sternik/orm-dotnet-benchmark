
BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5608/22H2/2022Update)
Intel Core i5-8400 CPU 2.80GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores
.NET SDK 9.0.100
  [Host] : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2 DEBUG [AttachedDebugger]

Job=MediumRun  Toolchain=InProcessNoEmitToolchain  InvocationCount=100  
IterationCount=1  LaunchCount=1  UnrollFactor=10  
WarmupCount=1  

 Method          | Mean     | Error | Gen0       | Gen1      | Gen2      | Allocated |
---------------- |---------:|------:|-----------:|----------:|----------:|----------:|
 EFCore_Postgres | 260.9 ms |    NA | 10870.0000 | 6100.0000 | 1320.0000 |  60.64 MB |
 EFCore_MSSQL    | 282.9 ms |    NA | 10810.0000 | 6040.0000 | 1250.0000 |  60.68 MB |
