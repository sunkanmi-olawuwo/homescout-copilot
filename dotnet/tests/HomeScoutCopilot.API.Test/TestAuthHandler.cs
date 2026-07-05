using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Test;

// Offline stand-in for Keycloak JWT validation (RagLab's pattern): a stub authentication scheme so
// [Authorize] endpoints can be exercised without a live Keycloak or real signing keys. The request
// headers steer the outcome:
//   - no X-Test-Subject               -> unauthenticated (NoResult) => RequireAuthorization 401
//   - X-Test-Subject: <sub>           -> authenticated with that subject + profile claims
//   - X-Test-Omit-Subject             -> authenticated but WITHOUT a subject (endpoint returns 401)
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string SubjectHeader = "X-Test-Subject";
    public const string OmitSubjectHeader = "X-Test-Omit-Subject";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var omitSubject = Request.Headers.ContainsKey(OmitSubjectHeader);
        var hasSubject = Request.Headers.TryGetValue(SubjectHeader, out var headerSubject)
            && !string.IsNullOrEmpty(headerSubject);

        // No credentials at all -> anonymous, so RequireAuthorization challenges (401).
        if (!omitSubject && !hasSubject)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>();
        if (!omitSubject)
        {
            claims.Add(new Claim("sub", headerSubject.ToString()));
            claims.Add(new Claim("email", "test.user@homescout.local"));
            claims.Add(new Claim("name", "Test User"));
        }

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
