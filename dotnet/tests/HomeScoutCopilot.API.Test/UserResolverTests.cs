using System.Security.Claims;
using HomeScoutCopilot.API.Service;
using Microsoft.Extensions.Caching.Memory;

namespace HomeScoutCopilot.API.Test;

// Unit tests for the ClaimsPrincipal -> internal user id resolver (the no-database / anonymous
// paths). The resolve-and-cache-over-a-real-directory case lives in PostgresUserDirectoryTests.
[TestFixture]
public class UserResolverTests
{
    private static IMemoryCache Cache() => new MemoryCache(new MemoryCacheOptions());

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    [Test]
    public async Task Returns_null_when_the_directory_is_disabled()
    {
        var resolver = new UserResolver(new NullUserDirectory(), Cache());

        var id = await resolver.ResolveUserIdAsync(Principal(new Claim("sub", "user-1")));

        Assert.That(id, Is.Null);
    }

    [Test]
    public async Task Returns_null_for_an_anonymous_principal()
    {
        // Even with an enabled directory, no subject claim -> no user.
        var resolver = new UserResolver(new NullUserDirectory(), Cache());

        var id = await resolver.ResolveUserIdAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.That(id, Is.Null);
    }
}
