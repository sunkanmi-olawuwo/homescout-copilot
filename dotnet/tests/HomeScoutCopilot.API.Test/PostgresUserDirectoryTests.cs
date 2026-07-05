using System.Security.Claims;
using HomeScoutCopilot.API.Service;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomeScoutCopilot.API.Test;

// Integration tests for the PostgreSQL user directory against a throwaway container. Category=Database
// (runs in the PR gate — hermetic; self-skips without Docker), like PostgresSessionStoreTests.
[TestFixture]
[Category("Database")]
public class PostgresUserDirectoryTests
{
    private PostgreSqlContainer _container = null!;
    private NpgsqlDataSource _dataSource = null!;
    private PostgresUserDirectory _directory = null!;

    [OneTimeSetUp]
    public async Task StartDatabase()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Docker/Testcontainers unavailable, skipping user-directory tests: {ex.Message}");
        }

        _dataSource = new NpgsqlDataSourceBuilder(_container.GetConnectionString()).Build();
        _directory = new PostgresUserDirectory(_dataSource);
        await _directory.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task StopDatabase()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SetUp]
    public async Task ClearTable()
    {
        await using var command = _dataSource.CreateCommand("DELETE FROM app_users");
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task Record_creates_a_user_and_returns_the_canonical_identity()
    {
        var user = await _directory.RecordAsync(UserIdentityProviders.Keycloak, "sub-1", "a@b.com", "Ada");

        Assert.That(user, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(user!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(user.Provider, Is.EqualTo("keycloak"));
            Assert.That(user.Subject, Is.EqualTo("sub-1"));
            Assert.That(user.Email, Is.EqualTo("a@b.com"));
            Assert.That(user.Name, Is.EqualTo("Ada"));
        });
    }

    [Test]
    public async Task Record_is_idempotent_for_the_same_provider_subject()
    {
        var first = await _directory.RecordAsync(UserIdentityProviders.Keycloak, "sub-1", "a@b.com", "Ada");
        var second = await _directory.RecordAsync(UserIdentityProviders.Keycloak, "sub-1", "a@b.com", "Ada");

        Assert.That(second!.Id, Is.EqualTo(first!.Id), "the same identity must resolve to one canonical id");
    }

    [Test]
    public async Task Record_fills_in_newly_known_email_and_name_but_keeps_the_id()
    {
        var first = await _directory.RecordAsync(UserIdentityProviders.Keycloak, "sub-1", email: null, name: null);
        var second = await _directory.RecordAsync(UserIdentityProviders.Keycloak, "sub-1", "later@b.com", "Ada Lovelace");

        Assert.Multiple(() =>
        {
            Assert.That(second!.Id, Is.EqualTo(first!.Id));
            Assert.That(second.Email, Is.EqualTo("later@b.com"));
            Assert.That(second.Name, Is.EqualTo("Ada Lovelace"));
        });
    }

    [Test]
    public async Task Different_subjects_get_different_ids()
    {
        var a = await _directory.RecordAsync(UserIdentityProviders.Keycloak, "sub-a", null, null);
        var b = await _directory.RecordAsync(UserIdentityProviders.Keycloak, "sub-b", null, null);

        Assert.That(a!.Id, Is.Not.EqualTo(b!.Id));
    }

    [Test]
    public async Task Resolver_resolves_a_principal_to_the_internal_id()
    {
        var resolver = new UserResolver(_directory, new MemoryCache(new MemoryCacheOptions()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "resolve-sub"), new Claim("email", "r@b.com"), new Claim("name", "Rey")], "test"));

        var first = await resolver.ResolveUserIdAsync(principal);
        var second = await resolver.ResolveUserIdAsync(principal);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null.And.Not.EqualTo(Guid.Empty));
            Assert.That(second, Is.EqualTo(first), "the same principal resolves to one cached id");
        });
    }

    [Test]
    public async Task Concurrent_first_sign_ins_converge_on_one_user()
    {
        // Mirror RagLab's UserDirectoryConcurrencyTests: many parallel first sightings of the same
        // subject must all succeed and resolve to exactly one row / one id (the unique index +
        // atomic upsert handle the race).
        const int parallelism = 12;
        var tasks = Enumerable.Range(0, parallelism)
            .Select(_ => _directory.RecordAsync(UserIdentityProviders.Keycloak, "race-sub", "r@b.com", "Racer"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Multiple(async () =>
        {
            Assert.That(results, Is.All.Not.Null);
            Assert.That(results.Select(r => r!.Id).Distinct().Count(), Is.EqualTo(1), "all callers see one id");

            await using var command = _dataSource.CreateCommand("SELECT count(*) FROM app_users WHERE subject = 'race-sub'");
            var count = (long)(await command.ExecuteScalarAsync())!;
            Assert.That(count, Is.EqualTo(1), "exactly one row exists for the subject");
        });
    }
}
