using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Resolves the authenticated caller (a <see cref="ClaimsPrincipal"/>) to HomeScout's internal user
/// id, so request handlers can stamp per-user data without touching claims or the DB directly. The
/// subject→id mapping is cached (the user directory already ensured the row exists via JIT capture),
/// so a hot request path doesn't hit the database every turn.
/// </summary>
public interface IUserResolver
{
    /// <summary>The internal user id for this principal, or null when anonymous / no database.</summary>
    Task<Guid?> ResolveUserIdAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

public sealed class UserResolver(IUserDirectory directory, IMemoryCache cache) : IUserResolver
{
    public async Task<Guid?> ResolveUserIdAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (!directory.IsEnabled)
        {
            return null;
        }

        var subject = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(subject))
        {
            return null;
        }

        var cacheKey = $"user-id:{UserIdentityProviders.Keycloak}:{subject}";
        if (cache.TryGetValue(cacheKey, out Guid cachedId))
        {
            return cachedId;
        }

        var email = principal.FindFirstValue("email");
        var name = principal.FindFirstValue("name") ?? principal.FindFirstValue("preferred_username");
        var record = await directory
            .RecordAsync(UserIdentityProviders.Keycloak, subject, email, name, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        cache.Set(cacheKey, record.Id, TimeSpan.FromMinutes(30));
        return record.Id;
    }
}
