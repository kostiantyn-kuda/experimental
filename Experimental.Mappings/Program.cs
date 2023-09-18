using System.Diagnostics;

var a = new MappingBenchmarks();

a.NumberOfRecords = 1_000_000;

a.Setup();
a.IterationSetup();

var sw = Stopwatch.StartNew();
a.WithAutomapper();
sw.Stop();
Console.WriteLine($"WithAutomapper: {sw.ElapsedMilliseconds}ms");


sw.Reset();
sw.Start();
a.WithMapperly();
sw.Stop();
Console.WriteLine($"WithMapperly: {sw.ElapsedMilliseconds}ms");

BenchmarkRunner.Run<MappingBenchmarks>();