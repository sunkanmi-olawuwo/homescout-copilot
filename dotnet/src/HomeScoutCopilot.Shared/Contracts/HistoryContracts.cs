namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>Metadata for one of a user's past conversations (no message content — the payload is the
/// opaque serialized session). A first-message preview/title is a future enhancement.</summary>
public record ConversationSummary(string SessionId, DateTimeOffset CreatedAt, DateTimeOffset LastActiveAt);

/// <summary>The signed-in user's conversation history, most-recent-first.</summary>
public record ConversationHistoryResponse(IReadOnlyList<ConversationSummary> Conversations);
