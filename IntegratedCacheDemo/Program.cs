using System;
using System.Threading.Tasks;

namespace IntegratedCacheDemo
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Benchmarks benchmarks = new Benchmarks();
            await benchmarks.RunBenchmark();
        }
    }
    class Benchmarks
    {
        PerformanceDemo performanceDemo = new PerformanceDemo();
        ItemCacheDemo itemCacheDemo = new ItemCacheDemo();
        QueryCacheDemo queryCacheDemo = new QueryCacheDemo();

        public async Task RunBenchmark()
        {
            bool exit = false;


            while (exit == false)
            {
                Console.Clear();
                Console.WriteLine($"Azure Cosmos DB Integrated Cache Demo");
                Console.WriteLine($"-----------------------------------------------------------");
                Console.WriteLine($"[1]   Measure cache performance");
                Console.WriteLine($"[2]   Understanding the Item cache");
                Console.WriteLine($"[3]   Understanding the Query cache");
                Console.WriteLine($"[4]   Initialize");
                Console.WriteLine($"[5]   Clean up");
                Console.WriteLine($"[6]   Exit");

                ConsoleKeyInfo result = Console.ReadKey(true);

                if (result.KeyChar == '1')
                {
                    Console.Clear();
                    await performanceDemo.RunBenchmarks();
                }
                else if (result.KeyChar == '2')
                {
                    await itemCacheDemo.RunBenchmarks();
                }
                else if (result.KeyChar == '3')
                {
                    await queryCacheDemo.RunBenchmarks();
                }
                else if (result.KeyChar == '4')
                {
                    Console.Clear();
                    await performanceDemo.Initialize();
                }
                else if (result.KeyChar == '5')
                {
                    Console.WriteLine("Running Clean up Routines");
                    await performanceDemo.CleanUp();
                }
                else if (result.KeyChar == '6')
                {
                    exit = true;
                }
            }
        }
    }
}
