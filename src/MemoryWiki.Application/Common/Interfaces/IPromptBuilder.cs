namespace MemoryWiki.Application.Common.Interfaces;

public sealed record Prompt(string System, string User);

/// <summary>Builds the deterministic system/user prompts for each LLM pass.</summary>
public interface IPromptBuilder
{
    Prompt BuildExtractPrompt(string transcriptTitle, string transcript);
    Prompt BuildComposePrompt(string? existingMarkdown, ExtractedMemory memory, string transcriptTitle);
}
