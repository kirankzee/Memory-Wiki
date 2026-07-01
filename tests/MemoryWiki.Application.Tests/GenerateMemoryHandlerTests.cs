using FluentAssertions;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Application.Memories.Commands;
using MemoryWiki.Application.Tests.Fakes;
using MemoryWiki.Contracts.Messages;
using MemoryWiki.Domain.Entities;
using MemoryWiki.Infrastructure.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MemoryWiki.Application.Tests;

public class GenerateMemoryHandlerTests
{
    private const string TranscriptText = "Alice: We will adopt RabbitMQ.\nBob: Sounds good, when?\nAlice: Next week.";

    private static (GenerateMemoryHandler handler, InMemoryObjectStorage storage, FakeTranscriptRepository transcripts, Mock<IMessagePublisher> publisher)
        Build()
    {
        var transcripts = new FakeTranscriptRepository();
        var jobs = new FakeJobRepository();
        var memories = new FakeMemoryRepository();
        var storage = new InMemoryObjectStorage();
        var publisher = new Mock<IMessagePublisher>();

        var t = Transcript.Create("Queue Migration", "transcripts/test.txt", 100, "hash");
        transcripts.Items[t.Id] = t;
        storage.UploadTextAsync(t.ObjectKey, TranscriptText).Wait();

        var handler = new GenerateMemoryHandler(transcripts, jobs, memories, storage, storage,
            new DeterministicGenerationService(), publisher.Object, new FakeUnitOfWork(),
            NullLogger<GenerateMemoryHandler>.Instance);

        return (handler, storage, transcripts, publisher);
    }

    [Fact]
    public async Task Handle_generates_memory_files_and_completes()
    {
        var (handler, storage, transcripts, publisher) = Build();
        var t = transcripts.Items.Values.First();

        var written = await handler.Handle(
            new GenerateMemoryCommand(t.Id, t.ObjectKey, "hash", null), default);

        written.Should().BeGreaterThan(0);
        (await storage.ListAllKeysAsync("people/")).Should().NotBeEmpty();
        t.Status.Should().Be(MemoryWiki.Domain.Enums.TranscriptStatus.Completed);
        publisher.Verify(p => p.PublishCompletedAsync(It.IsAny<MemoryCompletedMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_is_idempotent_on_reprocess()
    {
        var (handler, _, transcripts, _) = Build();
        var t = transcripts.Items.Values.First();
        var cmd = new GenerateMemoryCommand(t.Id, t.ObjectKey, "hash", null);

        await handler.Handle(cmd, default);
        var second = await handler.Handle(cmd, default);

        second.Should().Be(0); // already succeeded → no-op
    }
}
