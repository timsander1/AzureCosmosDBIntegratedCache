using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IntegratedCacheDemo
{
    class PerformanceDemo
    {
        private static List<Benchmark> benchmarks = null;
        public PerformanceDemo()
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                        .AddJsonFile("AppSettings.json")
                        .Build();

                //Define new Benchmarks
                benchmarks = new List<Benchmark>
                {
                    new Benchmark
                    {
                        benchmarkType = BenchmarkType.Write,
                        testName = "dedicated gateway",
                        testDescription = $"Write sample items",
                        accountEndpoint = configuration["dedicatedGatewayAccountEndpoint"],
                        accountKey = configuration["dedicatedGatewayAccountKey"],
                        databaseId = configuration["databaseId"],
                        containerId = configuration["containerId"],
                        partitionKeyPath = configuration["partitionKeyPath"],
                        partitionKeyValue = configuration["partitionKeyValue"],
                        connectionMode = ConnectionMode.Gateway
                    },

                    new Benchmark
                 {
                        benchmarkType = BenchmarkType.PointRead,
                        testName = "account without integrated cache",
                        testDescription = $"Do 100 test point reads without caching",
                        accountEndpoint = configuration["regularAccountEndpoint"],
                        accountKey = configuration["regularAccountKey"],
                        databaseId = configuration["databaseId"],
                        containerId = configuration["containerId"],
                        partitionKeyPath = configuration["partitionKeyPath"],
                        partitionKeyValue = configuration["partitionKeyValue"],
                        connectionMode = ConnectionMode.Gateway
                 },
                        new Benchmark
                 {
                        benchmarkType = BenchmarkType.PointRead,
                        testName = "account with integrated cache",
                        testDescription = $"Do 100 test point reads with caching",
                        accountEndpoint = configuration["dedicatedGatewayAccountEndpoint"],
                        accountKey = configuration["dedicatedGatewayAccountKey"],
                        databaseId = configuration["databaseId"],
                        containerId = configuration["containerId"],
                        partitionKeyPath = configuration["partitionKeyPath"],
                        partitionKeyValue = configuration["partitionKeyValue"],
                        connectionMode = ConnectionMode.Gateway
                 },
                        new Benchmark
                 {
                        benchmarkType = BenchmarkType.Query,
                        testName = "account without integrated cache",
                        testDescription = $"Do 100 test queries without caching",
                        accountEndpoint = configuration["re" +
                        "gularAccountEndpoint"],
                        accountKey = configuration["regularAccountKey"],
                        databaseId = configuration["databaseId"],
                        containerId = configuration["containerId"],
                        partitionKeyPath = configuration["partitionKeyPath"],
                        partitionKeyValue = configuration["partitionKeyValue"],
                        connectionMode = ConnectionMode.Gateway
                 },
                        new Benchmark
                 {
                        benchmarkType = BenchmarkType.Query,
                        testName = "account with integrated cache",
                        testDescription = $"Do 100 test queries with caching",
                        accountEndpoint = configuration["dedicatedGatewayAccountEndpoint"],
                        accountKey = configuration["dedicatedGatewayAccountKey"],
                        databaseId = configuration["databaseId"],
                        containerId = configuration["containerId"],
                        partitionKeyPath = configuration["partitionKeyPath"],
                        partitionKeyValue = configuration["partitionKeyValue"],
                        connectionMode = ConnectionMode.Gateway
                 }
                };

                foreach (Benchmark benchmark in benchmarks)
                {
                    benchmark.client = new CosmosClient(benchmark.accountEndpoint,benchmark.accountKey, new CosmosClientOptions { ConnectionMode = benchmark.connectionMode });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\nPress any key to continue");
                Console.ReadKey();
            }
        }
        public async Task Initialize()
        {
            try
            {
                foreach (Benchmark benchmark in benchmarks)
                {
                    await Benchmark.InitializeBenchmark(benchmark);
                }
            }
           catch (Exception e)
           {
               Console.WriteLine(e.Message + "\nPress any key to continue");
               Console.ReadKey();
            }
        }
        public async Task RunBenchmarks()
        {
            try
            {
                //Run benchmarks, collect results
                foreach (Benchmark benchmark in benchmarks)
                {
                    if (benchmark.benchmarkType == BenchmarkType.Write)
                        await Benchmark.WriteBenchmark(benchmark);
                    else if (benchmark.benchmarkType == BenchmarkType.Query)
                        await Benchmark.QueryBenchmark(benchmark);
                    else
                        await Benchmark.PointReadBenchmark(benchmark);
                }

                //Summarize the results
               
                foreach (Benchmark benchmark in benchmarks)
                {
                    ResultSummary r = benchmark.resultSummary;
                    Console.WriteLine("Test: {0,-26} Average Latency(ms): {1,-4} Average RU: {2,-4}", r.testName, r.averageLatency, r.averageRu);
                }
                Console.WriteLine($"\nTest concluded. Press any key to continue\n...");
                Console.ReadKey(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\nPress any key to continue");
                Console.ReadKey();
            }
        }
        public async Task CleanUp()
        {
            await Benchmark.CleanUp(benchmarks);
        }
    }
}

