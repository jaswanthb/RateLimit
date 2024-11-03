using Polly;
using Polly.RateLimiting;
using RateLimiter.Consumer;
using System.Threading.RateLimiting;

Console.WriteLine("Hello, World!");

var rlOptions = new FixedWindowRateLimiterOptions()
{
    AutoReplenishment = true,
    QueueLimit = 10,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    PermitLimit = 200,
    Window = TimeSpan.FromMinutes(1)
};

var pipeLine = new ResiliencePipelineBuilder()
                .AddConcurrencyLimiter(100, 50)
                .AddRateLimiter(new FixedWindowRateLimiter(rlOptions))

                //.AddRateLimiter(new SlidingWindowRateLimiter(rlOptions)) //for sliding ratelimiter
                //.AddRateLimiter(new RateLimiterStrategyOptions // for concurrency ratelimiter
                //{
                //    DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
                //    {
                //        PermitLimit = 10,
                        
                //    },
                //    OnRejected = args =>
                //    {
                //        Console.WriteLine("Rate limit has been exceeded");
                //        return default;
                //    }
                //})
                .Build();

try
{
    for (int i = 0; i < 100; i++)
    {
        var rlWapper = new RateLimitAPIWrapper();
        var result = await pipeLine.ExecuteAsync(async (token) => await rlWapper.CallRateLimitAPIWrapper(token));
        if (!result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {

            var retryAfter = result.Headers.GetValues("X-RateLimit-Reset").First();

            throw new RateLimiterRejectedException("", TimeSpan.FromSeconds(Convert.ToDouble(retryAfter)));
        }
    }
}
catch(RateLimiterRejectedException e)
{
    if (e.RetryAfter is TimeSpan retryAfter)
    {
        Console.WriteLine($"Retry After: {retryAfter}");
    }
}

Console.WriteLine("Done with loop");

