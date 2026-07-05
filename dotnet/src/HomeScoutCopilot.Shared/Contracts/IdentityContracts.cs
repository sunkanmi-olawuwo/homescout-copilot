namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>
/// The signed-in user's identity. <see cref="UserId"/> is HomeScout's internal, canonical user id
/// (what per-user data keys to); it is null when no database is configured (users aren't persisted).
/// <see cref="Subject"/> is the OIDC subject from the validated token.
/// </summary>
public record MeResponse(Guid? UserId, string Subject, string? Email, string? Name);
