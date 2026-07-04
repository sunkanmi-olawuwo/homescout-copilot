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
