using Microsoft.Extensions.Http;
using Polly;

Console.WriteLine("Hello, World!");

await InitiateMethod();

Console.WriteLine("End");
static async Task InitiateMethod()
{
    var policyHandler = new PolicyHttpMessageHandler(GetRetryPolicy()) { InnerHandler = new HttpClientHandler() };
    var httpClient = new HttpClient(policyHandler);

    for (int i = 0; i < 100; i++)
    {
        Console.WriteLine("Before: " + DateTime.Now);

        await httpClient.GetAsync("https://localhost:7037/WeatherForecast");

        Console.WriteLine("After: " + DateTime.Now);
    }
}

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy
       .HandleResult<HttpResponseMessage>(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
       .WaitAndRetryAsync(
           retryCount: 3,
           sleepDurationProvider: (retryAttempt, response, context) =>
           {
               if (response.Result.Headers.TryGetValues("X-RateLimit-Reset", out var values))
               {
                   var retryAfter = values.First();
                   if (int.TryParse(retryAfter, out int seconds))
                   {
                       return TimeSpan.FromSeconds(seconds);
                   }
               }
               return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
           },
           onRetryAsync: async (outcome, timespan, retryAttempt, context) =>
           {
               Console.WriteLine($"Retrying in {timespan.TotalSeconds} seconds... (Attempt {retryAttempt})");
               await Task.CompletedTask;
           });
}