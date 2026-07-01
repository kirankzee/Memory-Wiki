using MemoryWiki.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using Topology = MemoryWiki.Shared.Messaging;

namespace MemoryWiki.Infrastructure.Messaging;

/// <summary>
/// Shared, lazily-established RabbitMQ connection. Declares the exchange/queue
/// topology (with a dead-letter queue) once on first use.
/// </summary>
public sealed class RabbitMqConnection : IDisposable
{
    private readonly RabbitMqOptions _opt;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly Lazy<IConnection> _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _opt = options.Value;
        _logger = logger;
        _connection = new Lazy<IConnection>(CreateConnection);
    }

    public IConnection Connection => _connection.Value;

    private IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _opt.Host,
            Port = _opt.Port,
            UserName = _opt.Username,
            Password = _opt.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        // RabbitMQ may not be ready the instant the app starts; retry with backoff.
        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    _logger.LogWarning("Waiting for RabbitMQ (attempt {Attempt})…", args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        return pipeline.Execute(() => factory.CreateConnection("memorywiki"));
    }

    /// <summary>Idempotently declares exchanges and the work + dead-letter queues.</summary>
    public IModel CreateConfiguredChannel()
    {
        var channel = Connection.CreateModel();

        channel.ExchangeDeclare(Topology.Exchange, ExchangeType.Topic, durable: true);
        channel.ExchangeDeclare(Topology.DeadLetterExchange, ExchangeType.Topic, durable: true);

        channel.QueueDeclare(Topology.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(Topology.DeadLetterQueue, Topology.DeadLetterExchange, "#");

        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = Topology.DeadLetterExchange
        };
        channel.QueueDeclare(Topology.GenerateQueue, durable: true, exclusive: false, autoDelete: false, arguments: args);
        channel.QueueBind(Topology.GenerateQueue, Topology.Exchange, Topology.GenerateRoutingKey);

        return channel;
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated) _connection.Value.Dispose();
    }
}
