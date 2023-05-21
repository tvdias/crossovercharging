using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace CrossoverChargingTesting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Crossover Charging API Test");
            var summary = BenchmarkRunner.Run<MemcachedVsRedis>();
        }
    }

    [SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 20)]
    public class MemcachedVsRedis
    {
        private const string memcachedUrl = "https://vmw32ac5fb.execute-api.us-east-1.amazonaws.com/prod";
        private const string redisUrl = "https://k89yrd3snj.execute-api.us-east-1.amazonaws.com/prod";

        private readonly Uri memcachedChargeUrl = new(new Uri(memcachedUrl), "charge-request-memcached");
        private readonly Uri memcachedResetUrl = new(new Uri(memcachedUrl), "reset-memcached");
        private readonly Uri redisChargeUrl = new(new Uri(redisUrl), "charge-request-redis");
        private readonly Uri redisResetUrl = new(new Uri(redisUrl), "reset-redis");

        private readonly HttpClient client = new();
        private readonly StringContent requestBody = new("{\"serviceType\": \"voice\", \"unit\": 2}");

        public MemcachedVsRedis()
        {
            client.PostAsync(memcachedResetUrl, null);
            client.PostAsync(redisResetUrl, null);
        }

        [Benchmark]
        public Task<HttpResponseMessage> Memcached() => client.PostAsync(memcachedChargeUrl, requestBody);

        [Benchmark]
        public Task<HttpResponseMessage> Redis() => client.PostAsync(redisChargeUrl, requestBody);
    }
}

//StringContent requestBody = new StringContent("{\"serviceType\": \"voice\", \"unit\": 2}");

//do
//{
//    HttpClient client = createHttpClient();
//    var numberOfCalls = getNumberOfCalls();
//    //var interval = getCallsInterval();

//    Stopwatch timer = Stopwatch.StartNew();
//    List<Task<HttpResponseMessage>> calls = Enumerable.Repeat(client.PostAsync("", requestBody), numberOfCalls).ToList();

//    await Task.WhenAll(calls);

//    timer.Stop();

//    Console.WriteLine($"Took {timer.ElapsedMilliseconds}ms.");

//    client.Dispose();
//} while (true);

//HttpClient createHttpClient()
//{
//    Console.WriteLine("");
//    Console.WriteLine("Select either m for Memcached or r for Redis. Or e to Exit.");

//    var type = Console.ReadKey().Key;
//    string apiUrl = "";

//    switch (type)
//    {
//        case ConsoleKey.M:
//            apiUrl = "https://vmw32ac5fb.execute-api.us-east-1.amazonaws.com/prod/charge-request-memcached";
//            break;

//        case ConsoleKey.R:
//            apiUrl = "https://k89yrd3snj.execute-api.us-east-1.amazonaws.com/prod/charge-request-redis";
//            break;

//        case ConsoleKey.E:
//            Environment.Exit(0);
//            break;

//        default: return createHttpClient();
//    }

//    return new HttpClient()
//    {
//        BaseAddress = new Uri(apiUrl)
//    };
//}

//int getNumberOfCalls()
//{
//    Console.WriteLine("");
//    Console.WriteLine("Number of calls");
//    var numberOfCallsString = Console.ReadLine();

//    if (int.TryParse(numberOfCallsString, out var numberOfCalls))
//    {
//        return numberOfCalls;
//    }

//    return getNumberOfCalls();
//}
//int getCallsInterval()
//{
//    Console.WriteLine("Calls interval in ms");
//    var intervalString = Console.ReadLine();

//    if (int.TryParse(intervalString, out var interval))
//    {
//        return interval;
//    }

//    return getCallsInterval();
//}