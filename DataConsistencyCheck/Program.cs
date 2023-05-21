using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using Polly;

var numberSimultOfCalls = 4;
var numberOfExperiments = 5;
var memcachedUrl = new Uri("https://vmw32ac5fb.execute-api.us-east-1.amazonaws.com");
var redisUrl = new Uri("https://k89yrd3snj.execute-api.us-east-1.amazonaws.com");

Console.WriteLine("Crossover Charging API Test");

var retryPolicy = Policy.Handle<HttpRequestException>().Retry(3);

HttpClient client = new();
StringContent requestBody = new("{\"serviceType\": \"voice\", \"unit\": 2}");

client.Dispose();
client = new();

// run memcached
var warmupMemcached = await client.PostAsync(new Uri(memcachedUrl, "prod/charge-request-memcached"), requestBody);
var resetMemcached = await client.PostAsync(new Uri(memcachedUrl, "prod/reset-memcached"), null);

var timer = Stopwatch.StartNew();

for (int j = 0; j < numberOfExperiments; j++)
{

    var errorCountMemcached = 0;
    var balancesMemcached = new ConcurrentBag<int>();


    var memcachedCalls = new List<Task<HttpResponseMessage>>(numberSimultOfCalls);
    for (int i = 0; i < numberSimultOfCalls; i++)
    {
        var taskMemcached = retryPolicy.Execute(() => client
            .PostAsync(new Uri(memcachedUrl, "prod/charge-request-memcached"), requestBody));
        memcachedCalls.Add(taskMemcached);
    }

    while (memcachedCalls.Count > 0)
    {
        var task = await Task.WhenAny(memcachedCalls);
        memcachedCalls.Remove(task);
        if (!task.Result.IsSuccessStatusCode) errorCountMemcached++;

        var response = await task.Result.Content.ReadFromJsonAsync<ApiResponse>();
        balancesMemcached.Add(response.RemainingBalance);
    }

    if (errorCountMemcached > 0)
    {
        Console.WriteLine($"There was/were {errorCountMemcached} error(s) running Memcached requests.");
    }

    if (balancesMemcached.Distinct().Count() != balancesMemcached.Count)
    {
        Console.WriteLine($"There was an error on Memcached result. Balances:");
    }
    Console.WriteLine(string.Join(", ", balancesMemcached.OrderByDescending(r => r)));
}

timer.Stop();

Console.WriteLine($"Memcached took {timer.ElapsedMilliseconds}ms.");

client.Dispose();
client = new();

// run Redis
var warmupRedis = await client.PostAsync(new Uri(redisUrl, "prod/charge-request-redis"), requestBody);
var resetRedis = await client.PostAsync(new Uri(redisUrl, "prod/reset-redis"), null);

timer = Stopwatch.StartNew();

for (int j = 0; j < numberOfExperiments; j++)
{

    var errorCountRedis = 0;
    var balancesRedis = new ConcurrentBag<int>();


    var redisCalls = new List<Task<HttpResponseMessage>>(numberSimultOfCalls);
    for (int i = 0; i < numberSimultOfCalls; i++)
    {
        var taskRedis = retryPolicy.Execute(() => client
            .PostAsync(new Uri(redisUrl, "prod/charge-request-redis"), requestBody));
        redisCalls.Add(taskRedis);
    }

    while (redisCalls.Count > 0)
    {
        var task = await Task.WhenAny(redisCalls);
        redisCalls.Remove(task);
        if (!task.Result.IsSuccessStatusCode) errorCountRedis++;

        var response = await task.Result.Content.ReadFromJsonAsync<ApiResponse>();
        balancesRedis.Add(response.RemainingBalance);
    }

    if (errorCountRedis > 0)
    {
        Console.WriteLine($"There was/were {errorCountRedis} error(s) running Redis requests.");
    }

    if (balancesRedis.Distinct().Count() != balancesRedis.Count)
    {
        Console.WriteLine($"There was an error on Redis result. Balances:");
    }
    Console.WriteLine(string.Join(", ", balancesRedis.OrderByDescending(r => r)));
}

timer.Stop();

Console.WriteLine($"Redis took {timer.ElapsedMilliseconds}ms.");

client.Dispose();

internal class ApiResponse
{
    public int RemainingBalance { get; set; }

    public int Charges { get; set; }

    public bool IsAuthorized { get; set; }
}