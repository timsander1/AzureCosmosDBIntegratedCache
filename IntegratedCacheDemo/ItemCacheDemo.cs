using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace IntegratedCacheDemo
{
    class ItemCacheDemo
    {
            private static List<Benchmark> benchmarks = null;
            public ItemCacheDemo()
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
                        benchmarkType = BenchmarkType.CustomWrite,
                        testName = "Custom writes",
                        testDescription = $"Perform writes to a Cosmos DB account with an item cache",
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
                        benchmarkType = BenchmarkType.CustomPointRead,
                        testName = "Custom point reads",
                        testDescription = $"Perform point reads on a Cosmos DB account with an item cache",
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
                        benchmark.client = new CosmosClient(benchmark.accountEndpoint, benchmark.accountKey, new CosmosClientOptions { ConnectionMode = benchmark.connectionMode });
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
                        if (benchmark.benchmarkType == BenchmarkType.CustomWrite)
                            await Benchmark.CustomWriteBenchmark(benchmark);
                        else
                            await Benchmark.CustomPointReadBenchmark(benchmark);
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
