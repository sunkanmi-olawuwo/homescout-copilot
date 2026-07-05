using System.Text.Json;
using HomeScoutCopilot.API.Service;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomeScoutCopilot.API.Test;

// Integration tests for the real PostgreSQL durable session store, against a throwaway container.
// Category=Database (NOT Integration) so they run in the PR gate — Testcontainers Postgres is
// hermetic and deterministic (not a flaky third-party service), and GitHub runners have Docker.
// They self-skip when Docker isn't running, so local runs without Docker don't hard-fail.
[TestFixture]
[Category("Database")]
public class PostgresSessionStoreTests
{
    private static readonly ConversationOptions Options = new()
    {
        IdleTimeout = TimeSpan.FromHours(1),
        AbsoluteLifetime = TimeSpan.FromHours(24),
    };

    private PostgreSqlContainer _container = null!;
    private NpgsqlDataSource _dataSource = null!;
    private PostgresSessionStore _store = null!;

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
            // No Docker (common on dev machines) — skip rather than fail the suite.
            Assert.Ignore($"Docker/Testcontainers unavailable, skipping PostgreSQL store tests: {ex.Message}");
        }

        _dataSource = new NpgsqlDataSourceBuilder(_container.GetConnectionString()).Build();
        _store = new PostgresSessionStore(_dataSource, Microsoft.Extensions.Options.Options.Create(Options));
        await _store.InitializeAsync();
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
        // Each test starts from an empty table (Initialize already ran once).
        await using var command = _dataSource.CreateCommand("DELETE FROM conversation_sessions");
        await command.ExecuteNonQueryAsync();
    }

    private static JsonElement Payload(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Test]
    public void Store_reports_itself_persistent()
        => Assert.That(_store.IsPersistent, Is.True);

    [Test]
    public async Task Save_then_load_round_trips_the_payload()
    {
        await _store.SaveAsync("s1", Payload("""{"messages":["hi"],"turn":3}"""));

        var loaded = await _store.TryLoadAsync("s1");

        Assert.That(loaded, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(loaded!.Value.GetProperty("turn").GetInt32(), Is.EqualTo(3));
            Assert.That(loaded!.Value.GetProperty("messages")[0].GetString(), Is.EqualTo("hi"));
        });
    }

    [Test]
    public async Task Load_returns_null_for_an_unknown_session()
        => Assert.That(await _store.TryLoadAsync("missing"), Is.Null);

    [Test]
    public async Task Save_upserts_the_latest_payload()
    {
        await _store.SaveAsync("s1", Payload("""{"turn":1}"""));
        await _store.SaveAsync("s1", Payload("""{"turn":2}"""));

        var loaded = await _store.TryLoadAsync("s1");

        Assert.That(loaded!.Value.GetProperty("turn").GetInt32(), Is.EqualTo(2));
    }

    [Test]
    public async Task Remove_deletes_the_session()
    {
        await _store.SaveAsync("s1", Payload("""{"turn":1}"""));

        Assert.Multiple(async () =>
        {
            Assert.That(await _store.RemoveAsync("s1"), Is.True);
            Assert.That(await _store.TryLoadAsync("s1"), Is.Null);
            Assert.That(await _store.RemoveAsync("s1"), Is.False, "removing an absent session reports false");
        });
    }

    private async Task<Guid?> OwnerOf(string sessionId)
    {
        await using var command = _dataSource.CreateCommand("SELECT user_id FROM conversation_sessions WHERE session_id = @id");
        command.Parameters.AddWithValue("id", sessionId);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? null : (Guid)value;
    }

    [Test]
    public async Task Save_stamps_the_owner_and_a_later_anonymous_save_never_clears_it()
    {
        var owner = Guid.NewGuid();

        // Anonymous first turn: no owner.
        await _store.SaveAsync("s1", Payload("""{"turn":1}"""), userId: null);
        Assert.That(await OwnerOf("s1"), Is.Null);

        // Authenticated turn stamps the owner (the anon→auth hand-off).
        await _store.SaveAsync("s1", Payload("""{"turn":2}"""), userId: owner);
        Assert.That(await OwnerOf("s1"), Is.EqualTo(owner));

        // A subsequent anonymous save must NOT wipe the owner (COALESCE keeps it).
        await _store.SaveAsync("s1", Payload("""{"turn":3}"""), userId: null);
        Assert.That(await OwnerOf("s1"), Is.EqualTo(owner));
    }

    [Test]
    public async Task History_is_owner_scoped_and_never_leaks_another_users_sessions()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        await _store.SaveAsync("a1", Payload("""{"t":1}"""), userId: alice);
        await _store.SaveAsync("a2", Payload("""{"t":1}"""), userId: alice);
        await _store.SaveAsync("b1", Payload("""{"t":1}"""), userId: bob);
        await _store.SaveAsync("anon", Payload("""{"t":1}"""), userId: null);

        var aliceList = await _store.ListForUserAsync(alice, limit: 50);

        Assert.Multiple(async () =>
        {
            // Alice sees only her own two sessions — never Bob's or the anonymous one.
            Assert.That(aliceList.Select(c => c.SessionId), Is.EquivalentTo(new[] { "a1", "a2" }));
            // Single-conversation access is owner-scoped: Alice can fetch her own...
            Assert.That((await _store.GetForUserAsync("a1", alice))?.SessionId, Is.EqualTo("a1"));
            // ...but Bob's session is 404 (null) for Alice, and the anonymous one too.
            Assert.That(await _store.GetForUserAsync("b1", alice), Is.Null);
            Assert.That(await _store.GetForUserAsync("anon", alice), Is.Null);
        });
    }

    [Test]
    public async Task History_is_ordered_most_recently_active_first_and_capped()
    {
        var user = Guid.NewGuid();
        await _store.SaveAsync("older", Payload("""{"t":1}"""), userId: user);
        await Task.Delay(10);
        await _store.SaveAsync("newer", Payload("""{"t":1}"""), userId: user);

        var list = await _store.ListForUserAsync(user, limit: 1);

        // Only the single most-recently-active session comes back (order + cap).
        Assert.That(list.Single().SessionId, Is.EqualTo("newer"));
    }

    [Test]
    public async Task Sweep_keeps_fresh_sessions_but_evicts_idle_ones()
    {
        await _store.SaveAsync("s1", Payload("""{"turn":1}"""));

        // Sweeping at "now" leaves a just-saved session alone...
        var now = DateTimeOffset.UtcNow;
        Assert.That(await _store.SweepExpiredAsync(now), Is.EqualTo(0));

        // ...but sweeping as if an hour-plus has passed evicts it (idle > IdleTimeout).
        var later = now + Options.IdleTimeout + TimeSpan.FromMinutes(1);
        Assert.Multiple(async () =>
        {
            Assert.That(await _store.SweepExpiredAsync(later), Is.EqualTo(1));
            Assert.That(await _store.TryLoadAsync("s1"), Is.Null);
        });
    }
}
