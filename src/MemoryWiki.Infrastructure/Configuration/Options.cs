namespace MemoryWiki.Infrastructure.Configuration;

public sealed class StorageOptions
{
    public const string Section = "S3";
    public string ServiceUrl { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "memorywiki";
    public bool ForcePathStyle { get; set; } = true; // required for MinIO
    public string Region { get; set; } = "us-east-1";
}

public sealed class RabbitMqOptions
{
    public const string Section = "RabbitMQ";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public ushort PrefetchCount { get; set; } = 8;
    public int MaxRetries { get; set; } = 5;
}

public sealed class LlmOptions
{
    public const string Section = "Llm";
    public string Provider { get; set; } = "Deterministic"; // "Deterministic" | "OpenAI"
}

public sealed class OpenAiOptions
{
    public const string Section = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public double Temperature { get; set; } = 0.0; // deterministic output
    public int TimeoutSeconds { get; set; } = 60;
}
