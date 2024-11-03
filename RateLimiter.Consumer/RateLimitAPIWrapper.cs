namespace RateLimiter.Consumer;

internal class RateLimitAPIWrapper
{
    public async Task<HttpResponseMessage> CallRateLimitAPIWrapper(CancellationToken token, int i)
    {
        var client = new HttpClient();
        Console.WriteLine("calling api " + i + " time.\n");

        //This api is fixed rate limited to 2 calls in 15 sec
        return await client.GetAsync("https://localhost:7037/WeatherForecast", token); 
    }
}
