using Microsoft.Extensions.Http;
using Polly;
using Polly.RateLimiting;
using System.Threading.RateLimiting;

Console.WriteLine("Hello, World!");

await InitiateMethod();

Console.WriteLine("Done with loop");

async Task InitiateMethod()
{
    var pipeline = GetResiliencePipeline();
    try
    {
        for (int i = 0; i < 100; i++)
        {
            Console.WriteLine("Before: " + DateTime.Now);

            var result = await pipeline.ExecuteAsync(async (token) =>
                await CallRateLimitAPIWrapper(token, i)
            );

            Console.WriteLine("After: " + DateTime.Now);
            if (!result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var retryAfter = result.Headers.GetValues("X-RateLimit-Reset").First();
                //throw new RateLimiterRejectedException("", TimeSpan.FromSeconds(Convert.ToDouble(retryAfter)));
                Console.WriteLine("retryAfter: " + retryAfter);
            }
        }
    }
    catch (RateLimiterRejectedException e)
    {
        if (e.RetryAfter is TimeSpan retryAfter)
        {
            Console.WriteLine($"Retry After: {retryAfter}");
        }
    }
}

async Task<HttpResponseMessage> CallRateLimitAPIWrapper(CancellationToken token, int i)
{
    var policyHandler = new PolicyHttpMessageHandler(GetRetryPolicy()) { InnerHandler = new HttpClientHandler() };
    var httpClient = new HttpClient(policyHandler);
    Console.WriteLine("calling api " + i + " time.\n");

    //This api is fixed rate limited to 2 calls in 15 sec
    var res = await httpClient.GetAsync("https://localhost:7037/WeatherForecast", token);
    
    var retryAfter = res.Headers.Contains("X-RateLimit-Reset") ? res.Headers.GetValues("X-RateLimit-Reset").First() : null;
    Console.WriteLine("retryAfter: " + retryAfter);

    return res;
}

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    //return Policy
    //    .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    //    //.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(20));
    //    .WaitAndRetryAsync(3, (retryAttempt, response, context) =>
    //    {
    //        // Read the headers from the response
    //        var headers = response.Result.Headers;

    //        // Access the specific header value
    //        var headerValue = headers.GetValues("X-RateLimit-Reset").FirstOrDefault();

    //        // Pass the header value to the retry logic
    //        var delay = TimeSpan.FromSeconds(20);
    //        if (!string.IsNullOrEmpty(headerValue))
    //        {
    //            // Parse the header value and calculate the delay
    //            // For example, you can use the header value to determine the delay dynamically
    //            delay = TimeSpan.FromSeconds(int.Parse(headerValue));
    //        }

    //        // Return the delay for the retry attempt
    //        return delay;
    //    });

    return Policy
        .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: (retryCount, response, context) =>
            {
                var retryAfterFromResponse = response.Result.Headers.GetValues("X-RateLimit-Reset").FirstOrDefault();
                var waitDuration = Convert.ToDouble(retryAfterFromResponse);
                return TimeSpan.FromMilliseconds(waitDuration + 5);
            },
            onRetryAsync: async (response, timespan, retryCount, context) =>
            {
                /*logging*/
            }
        );
}

static ResiliencePipeline GetResiliencePipeline()
{
    var rlOptions = new FixedWindowRateLimiterOptions()
    {
        AutoReplenishment = true,
        QueueLimit = 10,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        PermitLimit = 2,
        Window = TimeSpan.FromSeconds(20)
    };

    return new ResiliencePipelineBuilder()
                    .AddRateLimiter(new FixedWindowRateLimiter(rlOptions))
                    .Build();
}