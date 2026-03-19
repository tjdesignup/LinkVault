using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using LinkVault.Application.Abstractions;

namespace LinkVault.Infrastructure.AI;

public sealed class AnthropicAiService(string apiKey) : IAiSummaryService
{
    private readonly AnthropicClient _client = new(apiKey);
    private const string Model = "claude-haiku-4-5-20251001";
    private const int MaxTokens = 1024;

    public async IAsyncEnumerable<string> SummarizeAsync(
        IEnumerable<string> linkDescriptions,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var descriptions = string.Join("\n", linkDescriptions.Select((d, i) => $"{i + 1}. {d}"));

        var messages = new List<Message>
        {
            new()
            {
                Role = RoleType.User,
                Content =
                [
                    new TextContent
                    {
                        Text = $"""
                            Zde je seznam uložených záložek uživatele:

                            {descriptions}

                            Napiš 3–4 věty o tom, co mají tyto záložky společného, jaké téma spojuje většinu z nich, a kterou bys doporučil přečíst jako první a proč. Odpověz česky.
                            """
                    }
                ]
            }
        };

        var parameters = new MessageParameters
        {
            Model = Model,
            MaxTokens = MaxTokens,
            Messages = messages,
            Stream = true
        };

        await foreach (var response in _client.Messages.StreamClaudeMessageAsync(parameters, cancellationToken))
        {
            if (response.Delta?.Text is { Length: > 0 } text)
                yield return text;
        }
    }
}