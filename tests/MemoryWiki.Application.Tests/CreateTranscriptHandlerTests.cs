using FluentAssertions;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Application.Tests.Fakes;
using MemoryWiki.Application.Transcripts.Commands;
using MemoryWiki.Contracts.Messages;
using Moq;
using Xunit;

namespace MemoryWiki.Application.Tests;

public class CreateTranscriptHandlerTests
{
    [Fact]
    public async Task Handle_persists_uploads_and_publishes()
    {
        var transcripts = new FakeTranscriptRepository();
        var jobs = new FakeJobRepository();
        var storage = new InMemoryObjectStorage();
        var uow = new FakeUnitOfWork();
        var publisher = new Mock<IMessagePublisher>();

        var handler = new CreateTranscriptHandler(transcripts, jobs, storage, publisher.Object, uow);

        var result = await handler.Handle(
            new CreateTranscriptCommand("Kickoff", "Alice: hello world this is a test.", null), default);

        result.Status.Should().Be("Queued");
        transcripts.Items.Should().ContainKey(result.Id);
        jobs.Items.Should().ContainSingle();
        storage.Count.Should().Be(1); // raw transcript stored
        publisher.Verify(p => p.PublishGenerateAsync(
            It.Is<GenerateMemoryMessage>(m => m.TranscriptId == result.Id), It.IsAny<CancellationToken>()), Times.Once);
    }
}
