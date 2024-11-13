using Microsoft.Extensions.Http;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RateLimit.Framework.Consumer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            InitiateMethod();

            Console.WriteLine("End");
        }

        static async Task InitiateMethod()
        {
            var policyHandler = new PolicyHttpMessageHandler(GetRateLimitPolicy()) { InnerHandler = new HttpClientHandler() };
            var httpClient = new HttpClient(policyHandler);

            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine("Before: " + DateTime.Now);
                try
                {
                    var res = httpClient.GetAsync("https://localhost:7037/WeatherForecast").Result;
                    Console.WriteLine("Response: " + res.Content.ReadAsStringAsync().Result);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }

                Console.WriteLine("After: " + DateTime.Now);
                
            }
        }

        static IAsyncPolicy<HttpResponseMessage> GetRateLimitPolicy()
        {
            //return Policy
            //    .RateLimitAsync<HttpResponseMessage>(1, TimeSpan.FromSeconds(1)); // Allow 1 request per second

            return Policy
               .HandleResult<HttpResponseMessage>(msg => msg.StatusCode == (System.Net.HttpStatusCode)429)
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
    }
}
