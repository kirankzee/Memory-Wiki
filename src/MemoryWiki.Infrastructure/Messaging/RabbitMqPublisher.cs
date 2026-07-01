using System.Text;
using System.Text.Json;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Contracts.Messages;
using RabbitMQ.Client;
using Topology = MemoryWiki.Shared.Messaging;

namespace MemoryWiki.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(RabbitMqConnection connection) : IMessagePublisher
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task PublishGenerateAsync(GenerateMemoryMessage message, CancellationToken ct = default)
        => Publish(Topology.GenerateRoutingKey, message, message.Id.ToString());

    public Task PublishCompletedAsync(MemoryCompletedMessage message, CancellationToken ct = default)
        => Publish(Topology.CompletedRoutingKey, message, message.TranscriptId.ToString());

    private Task Publish<T>(string routingKey, T message, string messageId)
    {
        using var channel = connection.CreateConfiguredChannel();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json));

        var props = channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2; // persistent
        props.MessageId = messageId;

        channel.BasicPublish(Topology.Exchange, routingKey, props, body);
        return Task.CompletedTask;
    }
}
