namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Anonymous conversation-session policy. Sessions are scoped to a browser via the HttpOnly
/// <see cref="CookieName"/> cookie (no auth), holding the agent's multi-turn state so follow-ups
/// keep context. Sliding idle + absolute cap bound memory and don't let a buyer's figures linger.
/// Defaults per the conversation-threads plan; all overridable via configuration.
/// </summary>
public sealed class ConversationOptions
{
    public static string SectionName => "Conversation";

    /// <summary>Evict a session after this much inactivity (sliding, reset each turn).</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>Evict a session this long after creation regardless of activity.</summary>
    public TimeSpan AbsoluteLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>How often the background sweeper evicts idle/expired sessions.</summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>The anonymous session cookie name.</summary>
    public string CookieName { get; set; } = "hs_session";
}
