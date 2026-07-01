using MemoryWiki.Contracts.Messages;

namespace MemoryWiki.Application.Common.Interfaces;

public interface IMessagePublisher
{
    Task PublishGenerateAsync(GenerateMemoryMessage message, CancellationToken ct = default);
    Task PublishCompletedAsync(MemoryCompletedMessage message, CancellationToken ct = default);
}
