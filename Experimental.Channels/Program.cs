// var benchmarks = new UsersHandlerBenchmarks
// {
//     MaxNumberOfUsers = 1000,
//     PageSize = 100
// };
//
// benchmarks.SetupBenchmark();
//
// //await benchmarks.AsyncHandlingWithChannels(3);
//
// await benchmarks.AsyncHandlingWithParallel();
//
// var a = 1;

BenchmarkRunner.Run<UsersHandlerBenchmarks>();