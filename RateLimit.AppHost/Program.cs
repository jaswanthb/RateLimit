var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RateLimit>("ratelimit");

//Core 8 consumer
//builder.AddProject<Projects.RateLimit_Consumer_Polly>("ratelimit-consumer-polly");

//Framework 4.8 consumer
builder.AddProject<Projects.RateLimit_Framework_Consumer>("ratelimit-framework-consumer");

await builder.Build().RunAsync();
