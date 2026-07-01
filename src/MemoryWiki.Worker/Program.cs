using MemoryWiki.Application;
using MemoryWiki.Infrastructure;
using MemoryWiki.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((sp, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MemoryGenerationConsumer>();

var host = builder.Build();
host.Run();
