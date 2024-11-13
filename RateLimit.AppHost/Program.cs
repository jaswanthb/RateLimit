var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RateLimit>("ratelimit");

builder.AddProject<Projects.RateLimit_Consumer_Polly>("ratelimit-consumer-polly");

await builder.Build().RunAsync();
