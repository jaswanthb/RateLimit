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
    PermitLimit = 2,
    Window = TimeSpan.FromMinutes(1)
};

var pipeLine = new ResiliencePipelineBuilder()
                .AddRateLimiter(new FixedWindowRateLimiter(rlOptions))
                .Build();

try
{
    for (int i = 0; i < 100; i++)
    {
        var rlWapper = new RateLimitAPIWrapper();
        var result = await pipeLine.ExecuteAsync(async (token) => await rlWapper.CallRateLimitAPIWrapper(token, i));
        if (!result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {

            //Need to figure out to read below header and reset retry time from the below header value

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