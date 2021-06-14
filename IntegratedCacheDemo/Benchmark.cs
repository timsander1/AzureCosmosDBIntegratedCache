using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Bogus.Extensions.Italy;
using Microsoft.Azure.Cosmos;

namespace IntegratedCacheDemo
{
    public enum BenchmarkType
    {
        Write = 0,
        PointRead = 1,
        Query = 2,
        CustomWrite = 3,
        CustomPointRead = 4,
        CustomQuery = 5
    }

    public class Benchmark
    {
        public BenchmarkType benchmarkType;
        public string testName;
        public string testDescription;
        public CosmosClient client;
        public Container container;
        public string accountEndpoint;
        public string accountKey;
        public string databaseId;
        public string containerId;
        public string partitionKeyPath;
        public string partitionKeyValue;
        public ItemRequestOptions itemRequestOptions;
        public QueryRequestOptions queryRequestOptions;
        public ConnectionMode connectionMode;
        public ResultSummary resultSummary;

        public static async Task InitializeBenchmark(Benchmark benchmark)
        {
                try
                {
                    benchmark.itemRequestOptions = new ItemRequestOptions { ConsistencyLevel = ConsistencyLevel.Eventual };
                    benchmark.queryRequestOptions = new QueryRequestOptions { ConsistencyLevel = ConsistencyLevel.Eventual };
                    benchmark.container = benchmark.client.GetContainer(benchmark.databaseId, benchmark.containerId);
                    await benchmark.container.ReadContainerAsync();  //ReadContainer to see if it is created
                }
                catch
                {
                    // If container has not been created, create it
                    Database database = await benchmark.client.CreateDatabaseIfNotExistsAsync(benchmark.databaseId);
                    Container container = await database.CreateContainerIfNotExistsAsync(benchmark.containerId, benchmark.partitionKeyPath, 6000);
                    benchmark.container = container;
                    
                    // Ingest some data
                    await InitialIngest(benchmark);
                }
        }
        public static async Task WriteBenchmark(Benchmark benchmark)
        {
            //Verify the benchmark is setup
            await InitializeBenchmark(benchmark);

            //Customers to insert
            List<SampleCustomer> customers = SampleCustomer.GenerateManyCustomers(benchmark.partitionKeyValue, 100);

            Console.WriteLine($"\n{benchmark.testDescription}\nPress any key to continue\n...");
            Console.ReadKey(true);

            int test = 1;
            List<Result> results = new List<Result>(); //Individual Benchmark results
            Stopwatch stopwatch = new Stopwatch();

            foreach (SampleCustomer customer in customers)
            {
                stopwatch.Start();
                ItemResponse<SampleCustomer> response = await benchmark.container.CreateItemAsync<SampleCustomer>(customer, new PartitionKey(benchmark.partitionKeyValue));
                stopwatch.Stop();

                Console.WriteLine($"Write {test++} of {customers.Count}, Latency: {stopwatch.ElapsedMilliseconds} ms, Request Charge: {response.RequestCharge} RUs");

                results.Add(new Result(stopwatch.ElapsedMilliseconds, response.RequestCharge));

                stopwatch.Reset();
            }

            OutputResults(benchmark, results);
        }
        public static async Task PointReadBenchmark(Benchmark benchmark)
        {
            //Verify the benchmark is setup
            await Benchmark.InitializeBenchmark(benchmark);

            //Fetch the id values to do point reads on
            List<string> ids = await GetIds(benchmark);

            foreach (string id in ids)
            {
                ItemResponse<SampleCustomer> response = await benchmark.container.ReadItemAsync<SampleCustomer>(id: id, partitionKey: new PartitionKey(benchmark.partitionKeyValue), benchmark.itemRequestOptions);
            }

            Console.WriteLine($"\n{benchmark.testDescription}\nPress any key to continue\n...");
            Console.ReadKey(true);

            int test = 1;
            List<Result> results = new List<Result>(); //Individual Benchmark results
            Stopwatch stopwatch = new Stopwatch();

            foreach (string id in ids)
            {
                stopwatch.Start();
                ItemResponse<SampleCustomer> response = await benchmark.container.ReadItemAsync<SampleCustomer>(id: id, partitionKey: new PartitionKey(benchmark.partitionKeyValue), benchmark.itemRequestOptions);
                stopwatch.Stop();

                Console.WriteLine($"Read {test++} of {ids.Count}, Latency: {stopwatch.ElapsedMilliseconds} ms, Request Charge: {response.RequestCharge} RUs");

                results.Add(new Result(stopwatch.ElapsedMilliseconds, response.RequestCharge));

                stopwatch.Reset();
            }

            OutputResults(benchmark, results);
        }

        public static async Task QueryBenchmark(Benchmark benchmark)
        {
            //Verify the benchmark is setup
            await Benchmark.InitializeBenchmark(benchmark);

            Console.WriteLine($"\n{benchmark.testDescription}\nPress any key to continue\n...");
            Console.ReadKey(true);

            int currentTest = 1;
            int totalTests = 100;

            List<Result> results = new List<Result>(); //Individual Benchmark results
            Stopwatch stopwatch = new Stopwatch();


            // Simple query
            QueryDefinition query = new QueryDefinition("SELECT TOP 10 c.id FROM c WHERE CONTAINS(UPPER(c.name), \"TIM\")");

            while (currentTest <= totalTests)
            {
                FeedIterator<SampleCustomer> resultSetIterator = benchmark.container.GetItemQueryIterator<SampleCustomer>(
                query, requestOptions: benchmark.queryRequestOptions);

                double requestCharge = 0;

                stopwatch.Start();

                // Code will work with queries with more than one page but not recommended for demo
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<SampleCustomer> response = await resultSetIterator.ReadNextAsync();
                    requestCharge = requestCharge + response.RequestCharge;
                }

                stopwatch.Stop();

                Console.WriteLine($"Query {currentTest++} of {totalTests}, Latency: {stopwatch.ElapsedMilliseconds} ms, Request Charge: {requestCharge} RUs");

                results.Add(new Result(stopwatch.ElapsedMilliseconds, requestCharge));

                stopwatch.Reset();
            }

            OutputResults(benchmark, results);
        }
        public static async Task CustomWriteBenchmark(Benchmark benchmark)
        {
            //Verify the benchmark is setup
            await InitializeBenchmark(benchmark);

            Console.WriteLine($"\n{benchmark.testDescription}\nPress any key to continue\n...");
            Console.ReadKey(true);

            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine($"\nWrite new item? Press 'n' to stop or any other key to write an item.");
                ConsoleKeyInfo result = Console.ReadKey(true);

                if (result.KeyChar == 'n')
                {
                    exit = true;
                    break;
                }

                Console.WriteLine($"\nEnter item id: ");
                string id = Console.ReadLine();

                Console.WriteLine($"\nEnter name value: ");
                string name = Console.ReadLine();

                //Single customer to insert
                List<SampleCustomer> customers = SampleCustomer.GenerateSingleCustomer(benchmark.partitionKeyValue, id, name);

                foreach (SampleCustomer customer in customers)
                {
                    ItemResponse<SampleCustomer> response = await benchmark.container.UpsertItemAsync<SampleCustomer>(customer, new PartitionKey(benchmark.partitionKeyValue));
                }
            }
        }

        public static async Task CustomPointReadBenchmark(Benchmark benchmark)
        {
            //Verify the benchmark is setup
            await Benchmark.InitializeBenchmark(benchmark);

            Console.Clear();

            Console.WriteLine($"\n{benchmark.testDescription}\nPress any key to continue\n...");
            Console.ReadKey(true);
            Console.Clear();

            bool exit = false;
            
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine($"\nPerform a point read? Press 'n' to stop or any other key to continue.");
                ConsoleKeyInfo result = Console.ReadKey(true);

                if (result.KeyChar == 'n')
                {
                    exit = true;
                    break;
                }

                Console.WriteLine($"\nTo perform a point read using the item cache press 'y'. To perform a point read using the backend data press 'n'");
                
                bool isValid = false;
                while (isValid == false)
                {
                    ConsoleKeyInfo optForCache = Console.ReadKey(true);                   

                    if (optForCache.KeyChar == 'y')
                    {
                        Console.WriteLine($"y");
                        benchmark.itemRequestOptions = new ItemRequestOptions { ConsistencyLevel = ConsistencyLevel.Eventual };
                        isValid = true;
                    }
                    else if (optForCache.KeyChar == 'n')
                    {
                        Console.WriteLine($"n");
                        benchmark.itemRequestOptions = new ItemRequestOptions { ConsistencyLevel = ConsistencyLevel.Session };
                        isValid = true;
                    }
                }

                Console.WriteLine($"\nEnter item id: ");
                string id = Console.ReadLine();

                try
                {
                    ItemResponse<SampleCustomer> response = await benchmark.container.ReadItemAsync<SampleCustomer>(id: id, partitionKey: new PartitionKey(benchmark.partitionKeyValue), requestOptions: benchmark.itemRequestOptions);
                    Console.WriteLine($"\nRead item with id: {response.Resource.Id} and name: {response.Resource.Name}");
                    Console.WriteLine($"\nRequest charge: {response.RequestCharge} RU \n");
                }
                catch {
                    Console.WriteLine($"\nItem with id: {id} does not exist.");
                }

                Console.WriteLine($"\nPress any key to continue\n...");
                Console.ReadKey(true);
            }
        }

        public static async Task CustomQueryBenchmark(Benchmark benchmark)
        {
            //Verify the benchmark is setup
            await Benchmark.InitializeBenchmark(benchmark);
            Console.Clear();

            Console.WriteLine($"\n{benchmark.testDescription}\nPress any key to continue\n...");
            Console.ReadKey(true);

            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine($"\nPerform a query? Press 'n' to stop or any other key to continue.");
                ConsoleKeyInfo result = Console.ReadKey(true);
                Console.WriteLine($"{result.KeyChar.ToString()}");

                if (result.KeyChar == 'n')
                {
                    exit = true;
                    break;
                }

                Console.WriteLine($"\nTo perform a query using the query cache press 'y'. To perform a query using the backend data press 'n'");

                bool isValid = false;

                while (isValid == false)
                {
                    ConsoleKeyInfo optForCache = Console.ReadKey(true);

                    if (optForCache.KeyChar == 'y')
                    {
                        Console.WriteLine($"y");
                        benchmark.queryRequestOptions = new QueryRequestOptions { ConsistencyLevel = ConsistencyLevel.Eventual };
                        isValid = true;
                    }
                    else if (optForCache.KeyChar == 'n')
                    {
                        Console.WriteLine($"n");
                        benchmark.queryRequestOptions = new QueryRequestOptions { ConsistencyLevel = ConsistencyLevel.Session };
                        isValid = true;
                    }
                }

                Console.WriteLine($"\nEnter query: ");
                string queryText = Console.ReadLine();


                try
                {
                    QueryDefinition query = new QueryDefinition(queryText);

                    FeedIterator<SampleCustomer> resultSetIterator = benchmark.container.GetItemQueryIterator<SampleCustomer>(
                        query, requestOptions: benchmark.queryRequestOptions);

                    double requestCharge = 0;

                    while (resultSetIterator.HasMoreResults)
                    {
                        FeedResponse<SampleCustomer> response = await resultSetIterator.ReadNextAsync();
                        requestCharge = requestCharge + response.RequestCharge;
                    }
                    Console.WriteLine($"\nRequest charge: {requestCharge} RUs \n");
                }

                catch
                {
                    Console.WriteLine($"Invalid query syntax.");
                }

                Console.WriteLine($"\nPress any key to continue\n...");
                Console.ReadKey(true);
            }
        }

        public static async Task InitialIngest(Benchmark benchmark)
        {
            //Verify the benchmark is setup
            await InitializeBenchmark(benchmark);

            new CosmosClient(benchmark.accountEndpoint, benchmark.accountKey, new CosmosClientOptions { ConnectionMode = ConnectionMode.Direct, AllowBulkExecution = true});

            int numberOfItems = 1000;
            int totalItemsUploadedSoFar = 0;

            //Customers to insert
            List<SampleCustomer> customers = SampleCustomer.GenerateManyCustomers(benchmark.partitionKeyValue, numberOfItems);

            Console.WriteLine($"\n Initial Data Ingest \nPress any key to continue\n...");
            Console.ReadKey(true);

            foreach (SampleCustomer customer in customers)
            {
                ItemResponse<SampleCustomer> response = await benchmark.container.CreateItemAsync<SampleCustomer>(customer, new PartitionKey(benchmark.partitionKeyValue));
                totalItemsUploadedSoFar++;
                if (totalItemsUploadedSoFar % 100 == 0)
                {
                    Console.WriteLine($"Bulk inserted {totalItemsUploadedSoFar} items into a Cosmos container");
                }
            }
        }

        public static async Task<List<string>> GetIds(Benchmark benchmark)
        {
            List<string> results = new List<string>();
            QueryDefinition query = new QueryDefinition("SELECT top 100 value c.id FROM c ORDER BY c._ts");

            FeedIterator<string> resultSetIterator = benchmark.container.GetItemQueryIterator<string>(
                query, requestOptions: new QueryRequestOptions() { PartitionKey = new PartitionKey(benchmark.partitionKeyValue) });

            while (resultSetIterator.HasMoreResults)
            {
                FeedResponse<string> response = await resultSetIterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        private static string GetBenchmarkType(Benchmark benchmark)
        {
            string benchmarkType = string.Empty;

            switch (benchmark.benchmarkType)
            {
                case BenchmarkType.Write:
                    benchmarkType = "Writes";
                    break;
                case BenchmarkType.PointRead:
                    benchmarkType = "Point Reads";
                    break;
                case BenchmarkType.Query:
                    benchmarkType = "Queries";
                    break;
                case BenchmarkType.CustomWrite:
                    benchmarkType = "CustomWrite";
                    break;
                case BenchmarkType.CustomPointRead:
                    benchmarkType = "CustomPointRead";
                    break;
                case BenchmarkType.CustomQuery:
                    benchmarkType = "CustomQuery";
                    break;
            }
            return benchmarkType;
        }
        private static void OutputResults(Benchmark benchmark, List<Result> results)
        {
            string benchmarkType = GetBenchmarkType(benchmark);
            string testName = benchmark.testName;

            //Average at 99th Percentile
            string averageLatency = Math.Round(results.OrderBy(o => o.Latency).Take(99).Average(o => o.Latency), 1).ToString();
            string averageRu = Math.Round(results.OrderBy(o => o.Latency).Take(99).Average(o => o.RU), 1).ToString();

            //Save summary back to benchmark
            benchmark.resultSummary = new ResultSummary(benchmark.testName, averageLatency, averageRu);

            Console.WriteLine($"\nSummary\n");
            Console.WriteLine($"Test {results.Count} {benchmarkType} with {testName}\n");
            Console.WriteLine($"Average Latency:\t{averageLatency} ms");
            Console.WriteLine($"Average Request Units:\t{averageRu} RUs\n\nPress any key to continue...\n");
            Console.ReadKey(true);
        }


        public static async Task CleanUp(List<Benchmark> benchmarks)
        {
            try
            {
                foreach (Benchmark benchmark in benchmarks)
                {
                    await benchmark.client.GetDatabase(benchmark.databaseId).DeleteAsync();
                }
            }
            catch { }
        }
    }
    class Result
    {
        public long Latency;
        public double RU;

        public Result(long latency, double ru)
        {
            Latency = latency;
            RU = ru;
        }
    }
    public class ResultSummary
    {
        public string testName;
        public string averageLatency;
        public string averageRu;

        public ResultSummary(string test, string latency, string Ru)
        {
            testName = test;
            averageLatency = latency;
            averageRu = Ru;
        }
    }
}
