using System.Text;
using System.Text.Json;
using MediatR;
using MemoryWiki.Application.Memories.Commands;
using MemoryWiki.Contracts.Messages;
using MemoryWiki.Domain.Enums;
using MemoryWiki.Domain.Repositories;
using MemoryWiki.Infrastructure.Configuration;
using MemoryWiki.Infrastructure.Messaging;
using MemoryWiki.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Topology = MemoryWiki.Shared.Messaging;

namespace MemoryWiki.Worker;

/// <summary>
/// Consumes <c>memory.generate</c> messages and runs the generation pipeline.
/// Reliability model:
///  - manual acknowledgements (at-least-once delivery)
///  - idempotency via the per-transcript ProcessingJob (re-delivery is a safe no-op)
///  - bounded retries with backoff; exhausted jobs are dead-lettered and acked.
/// </summary>
public sealed class MemoryGenerationConsumer(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<MemoryGenerationConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _opt = options.Value;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Make sure migrations are applied and the bucket exists before consuming.
        await InfrastructureInitializer.InitializeAsync(scopeFactory.CreateScope().ServiceProvider, stoppingToken);

        _channel = connection.CreateConfiguredChannel();
        _channel.BasicQos(0, _opt.PrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnReceivedAsync;

        _channel.BasicConsume(Topology.GenerateQueue, autoAck: false, consumer);
        logger.LogInformation("Worker listening on {Queue} (prefetch {Prefetch}).", Topology.GenerateQueue, _opt.PrefetchCount);

        // Keep the service alive until cancellation.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = _channel!;
        GenerateMemoryMessage? message = null;
        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            message = JsonSerializer.Deserialize<GenerateMemoryMessage>(body, Json);
            if (message is null) throw new InvalidOperationException("Unparseable message.");

            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var files = await mediator.Send(new GenerateMemoryCommand(
                message.TranscriptId, message.ObjectKey, message.IdempotencyKey, message.TenantId));

            channel.BasicAck(ea.DeliveryTag, multiple: false);
            logger.LogInformation("Acked transcript {Id} ({Files} files).", message.TranscriptId, files);
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(channel, ea, message, ex);
        }
    }

    private async Task HandleFailureAsync(IModel channel, BasicDeliverEventArgs ea, GenerateMemoryMessage? message, Exception ex)
    {
        // Poison/unparseable messages: dead-letter immediately (requeue:false → DLX).
        if (message is null)
        {
            logger.LogError(ex, "Dropping unparseable message to dead-letter queue.");
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        var deadLettered = await IsDeadLetteredAsync(message.TranscriptId);
        if (deadLettered)
        {
            logger.LogError(ex, "Transcript {Id} exhausted retries; dead-lettering.", message.TranscriptId);
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // → DLX
        }
        else
        {
            logger.LogWarning(ex, "Transcript {Id} failed; requeueing for retry.", message.TranscriptId);
            await Task.Delay(TimeSpan.FromSeconds(2));
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task<bool> IsDeadLetteredAsync(Guid transcriptId)
    {
        using var scope = scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var job = await jobs.GetByTranscriptIdAsync(transcriptId);
        return job is { Status: JobStatus.DeadLettered };
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
