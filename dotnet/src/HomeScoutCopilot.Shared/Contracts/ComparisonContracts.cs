namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>Product identity and direction returned by <c>GET /api/status</c>.</summary>
public record HomeScoutStatus(string Product, string Frontend, string Architecture, string AgentPlatform);
