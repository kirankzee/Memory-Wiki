using Amazon.Runtime;
using Amazon.S3;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Domain.Repositories;
using MemoryWiki.Infrastructure.Configuration;
using MemoryWiki.Infrastructure.Llm;
using MemoryWiki.Infrastructure.Messaging;
using MemoryWiki.Infrastructure.Persistence;
using MemoryWiki.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace MemoryWiki.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // --- Options ---
        services.Configure<StorageOptions>(config.GetSection(StorageOptions.Section));
        services.Configure<RabbitMqOptions>(config.GetSection(RabbitMqOptions.Section));
        services.Configure<LlmOptions>(config.GetSection(LlmOptions.Section));
        services.Configure<OpenAiOptions>(config.GetSection(OpenAiOptions.Section));

        // --- PostgreSQL / EF Core ---
        var connectionString = config.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=memorywiki;Username=memorywiki;Password=memorywiki";
        services.AddDbContext<MemoryWikiDbContext>(o => o.UseNpgsql(connectionString, npg =>
            npg.MigrationsAssembly(typeof(MemoryWikiDbContext).Assembly.FullName)));

        services.AddScoped<ITranscriptRepository, TranscriptRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // --- Object storage (MinIO / S3) ---
        var storage = config.GetSection(StorageOptions.Section).Get<StorageOptions>() ?? new StorageOptions();
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            new BasicAWSCredentials(storage.AccessKey, storage.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = storage.ServiceUrl,
                ForcePathStyle = storage.ForcePathStyle,
                AuthenticationRegion = storage.Region
            }));
        services.AddScoped<S3ObjectStorage>();
        services.AddScoped<IObjectStorage>(sp => sp.GetRequiredService<S3ObjectStorage>());
        services.AddScoped<ITranscriptContentReader>(sp => sp.GetRequiredService<S3ObjectStorage>());

        // --- Messaging (RabbitMQ) ---
        services.AddSingleton<RabbitMqConnection>();
        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

        // --- LLM / generation ---
        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        AddGeneration(services, config);

        return services;
    }

    private static void AddGeneration(IServiceCollection services, IConfiguration config)
    {
        var llm = config.GetSection(LlmOptions.Section).Get<LlmOptions>() ?? new LlmOptions();
        var openAi = config.GetSection(OpenAiOptions.Section).Get<OpenAiOptions>() ?? new OpenAiOptions();

        var useOpenAi = string.Equals(llm.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(openAi.ApiKey);

        if (useOpenAi)
        {
            services.AddHttpClient<IGenerationService, OpenAiGenerationService>(c =>
            {
                c.BaseAddress = new Uri(openAi.BaseUrl.TrimEnd('/') + "/");
                c.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAi.ApiKey}");
                c.Timeout = TimeSpan.FromSeconds(openAi.TimeoutSeconds);
            })
            // Retry transient failures + 429 rate limits with exponential backoff.
            .AddTransientHttpErrorPolicy(p => p
                .OrResult(r => (int)r.StatusCode == 429)
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
        }
        else
        {
            services.AddSingleton<IGenerationService, DeterministicGenerationService>();
        }
    }
}
