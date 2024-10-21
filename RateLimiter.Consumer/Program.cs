using Microsoft.TeamFoundation.Build.WebApi;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Net.Http.Headers;

Console.WriteLine("Hello, World!");

//HttpClient _httpClient;
AsyncRetryPolicy _retryPolicy;
AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

_retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<RateLimitExceededException>() // Handle rate limit exception
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(10));

_circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(3, TimeSpan.FromMinutes(10));

using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

for (int i = 0; i < 100; i++)
{
    await _circuitBreakerPolicy.ExecuteAsync(async () =>
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await client.GetAsync("https://localhost:7037/WeatherForecast");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            return await response.Content.ReadAsStringAsync();
        });
    });
}

Console.WriteLine("Done with loop");