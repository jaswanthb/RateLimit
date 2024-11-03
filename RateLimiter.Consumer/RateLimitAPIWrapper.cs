namespace RateLimiter.Consumer;

internal class RateLimitAPIWrapper
{
    public async Task<HttpResponseMessage> CallRateLimitAPIWrapper(CancellationToken token)
    {
        var client = new HttpClient();
        return await client.GetAsync("https://localhost:7037/WeatherForecast", token);
    }
}
