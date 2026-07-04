using Microsoft.Extensions.AI;

namespace HomeScoutCopilot.Evaluation.Test;

/// <summary>
/// Strips an explicit <see cref="ChatOptions.Temperature"/> from every request so the judge model
/// uses its default. The Foundry <c>chat</c> deployment is a gpt-5-family reasoning model, which
/// rejects any temperature other than the default 1 (HTTP 400 unsupported_value). The built-in
/// <see cref="Microsoft.Extensions.AI.Evaluation.Quality"/> evaluators hard-code temperature 0 for
/// determinism, so without this shim every model-graded metric fails against a reasoning model.
/// </summary>
internal sealed class DefaultTemperatureChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(messages, WithDefaultTemperature(options), cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(messages, WithDefaultTemperature(options), cancellationToken);

    private static ChatOptions? WithDefaultTemperature(ChatOptions? options)
    {
        if (options?.Temperature is null)
        {
            return options;
        }

        // Clone so we never mutate the caller's options, then let the model apply its default.
        var normalized = options.Clone();
        normalized.Temperature = null;
        return normalized;
    }
}
