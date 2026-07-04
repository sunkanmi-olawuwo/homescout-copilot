# Keycloak Auth + Per-User History Plan

**Status:** Design-first (not started). Persistence-track **step 6** — the last item in
[[Conversation Threads — Multi-Turn, Anonymous]]. Anonymous multi-turn threads + the durable
PostgreSQL session store are done and live-verified (2026-07-05); this slice adds end-user sign-in
and makes conversation history **per-user and cross-device**.

**Owning context:** the [[Plan Divergence]] decision *End-User Auth Uses Keycloak, Not Entra ID*
(2026-07-04) and the durable store in [[Conversation Threads — Multi-Turn, Anonymous]]. **Modelled
directly on the RagLab (`HBK.Insights.Raglab`) implementation** — Aspire-hosted Keycloak, JWT-bearer
API auth, and an external `(Provider, Subject)` OIDC identity resolved to an internal `User.Id`. The
concrete RagLab files to mirror are named inline below.

## Goal

A signed-in buyer/renter sees **their** conversation history on any device; an anonymous visitor
keeps working exactly as today (session-scoped, no login). One clean API seam so Codex builds login
in parallel.

## Scope & non-goals

- **In scope:** Keycloak as an Aspire resource; API JWT-bearer validation; `sub` → internal user id;
  associating durable sessions with a user; a per-user history endpoint; a `GET /api/me` identity
  endpoint; the frontend auth contract; the anonymous→authenticated hand-off.
- **Non-goals (deferred):** roles/permissions beyond ownership, IdP federation, email-verification
  flows, account-management UI, GDPR export/delete tooling, refresh-token-rotation tuning.
- **Explicitly unchanged:** **Azure resource access stays Entra managed identity /
  `DefaultAzureCredential`** (keyless Foundry + storage RBAC). Keycloak is *only* end-user login;
  the two coexist (see [[Plan Divergence]] and [[Server-Side Tools — OpenAPI Tools on the Foundry Agent]]).

## Key divergences from RagLab (decide up front)

RagLab is the blueprint, but HomeScout differs deliberately in three ways:

1. **Anonymous-capable, not all-authenticated.** RagLab puts `.RequireAuthorization()` on every
   route group. HomeScout must keep `/api/copilot/ask` working **without** login (the copilot is
   public); only the per-user history endpoints require a token. Auth is *additive*.
2. **Anonymous→authenticated hand-off.** RagLab has no anonymous data. HomeScout must let a visitor
   chat anonymously and, on login, **claim** that in-progress session — new behaviour we design here.
3. **Raw Npgsql, not EF Core, for the users table.** RagLab uses
   `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` + a `DbContext`. HomeScout's durable store already
   uses **raw `NpgsqlDataSource`** (`PostgresSessionStore`); the `app_users` table is one more small
   table, so we stay raw-Npgsql for consistency and dependency weight — and Postgres's atomic
   `INSERT … ON CONFLICT … RETURNING` handles the first-sign-in race in one statement (simpler than
   RagLab's EF catch-reload-retry, same guarantee).

## Architecture — moving parts

### 1. Aspire hosting (AppHost) — model: `HBK.Insights.RagLab.AppHost/AppHost.cs`

- Add `Aspire.Hosting.Keycloak` — pin **`13.4.5-preview.1.26316.12`** (matches RagLab and our 13.4.x
  line; it is a **preview** package — re-check for a stable release at build time and pin
  deliberately per the engineering standards).
- **Keycloak keeps its own storage** (its embedded DB + a data volume) — it does *not* share the
  app's `sessions`/app Postgres. Mirrors RagLab exactly:
  ```csharp
  var keycloak = builder.AddKeycloak("keycloak")
      .WithDataVolume("homescout-keycloak-data")
      .WithRealmImport("./keycloak");          // directory holding realm-export.json
  // existing app Postgres is unchanged; the API references BOTH.
  apiService.WithReference(keycloak).WaitFor(keycloak);
  ```
  (RagLab also sets `KC_HOSTNAME_STRICT=false` / `KC_HTTP_ENABLED=true` and an explicit HTTP
  endpoint for dev; adopt if the default Aspire endpoint wiring needs it.)
- The existing `AddPostgres("postgres").AddDatabase("sessions")` stays app-only.

### 2. Realm & clients — committed export imported via `WithRealmImport`

- Commit `keycloak/realm-export.json` (a directory `keycloak/` passed to `WithRealmImport`) so the
  realm/clients are reproducible and diff-reviewable — no click-ops. Model:
  `HBK.Insights.RagLab.AppHost/KeycloakRealm/realm-export.json`.
- **Realm:** `homescout`.
- **Clients** (RagLab ships three; adopt the shape):
  - `homescout-api` — the API. `bearerOnly: true`, confidential, with an **audience mapper** that
    stamps `homescout-api` into access tokens (so the API can require that audience).
  - `homescout-web` — the React SPA. `publicClient: true`, standard flow, **PKCE `S256`**, redirect
    URIs + web origins for the Vite dev origin and the deployed origin.
  - *(optional)* `homescout-swagger` — public + PKCE, to authorize the OpenAPI/Scalar docs.
- Seed **test users only**, clearly marked (RagLab seeds `dev`/`dev` etc.). No real users committed.

### 3. API authentication — model: `HBK.Insights.RagLab.API/Component.cs`

- Packages: `Aspire.Keycloak.Authentication` **`13.4.5-preview.1.26316.12`** +
  `Microsoft.AspNetCore.Authentication.JwtBearer` (`10.0.x`, match the pinned ASP.NET version).
  ```csharp
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddKeycloakJwtBearer("keycloak", realm: "homescout", options =>
      {
          options.Audience = "homescout-api";
          options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
          options.Events = new JwtBearerEvents { OnTokenValidated = RecordAuthenticatedUserAsync };
      });
  builder.Services.AddAuthorization();
  // …
  app.UseAuthentication();
  app.UseAuthorization();
  ```
- **JIT user capture** (`OnTokenValidated`, RagLab's pattern): on a validated token, best-effort
  upsert the `(provider="keycloak", subject=sub)` user — **throttled** via `IMemoryCache`
  (`user-seen:{provider}:{subject}`, ~10 min) so we don't hammer the DB, and **never** fail auth if
  the capture errors. Optional claims `email`, `name`.
- **Anonymous-by-default** (divergence): endpoints are anonymous unless they opt in. Only history
  endpoints call `.RequireAuthorization()`. `/api/copilot/ask` accepts a bearer token *if present*
  (→ associate the turn with the user) but works without one (→ anonymous, unchanged).

### 4. Identity resolution — `(Provider, Subject)` → internal `User.Id`

Models: `RagLab.API/Domain/User.cs`, `Infrastructure/UserDirectory.cs`, `Infrastructure/UserResolver.cs`.

- Table (raw Npgsql, created by an initializer like `PostgresSessionStore`):
  ```sql
  app_users(
    id uuid PRIMARY KEY,
    provider text NOT NULL,
    subject text NOT NULL,
    email text NULL,
    name text NULL,
    first_seen_at timestamptz NOT NULL,
    last_seen_at timestamptz NOT NULL,
    UNIQUE(provider, subject)
  );
  ```
- `IUserDirectory.RecordAsync(provider, subject, email, name)` — atomic get-or-create, race-safe in
  one statement:
  ```sql
  INSERT INTO app_users (id, provider, subject, email, name, first_seen_at, last_seen_at)
  VALUES (@id, @provider, @subject, @email, @name, @now, @now)
  ON CONFLICT (provider, subject)
    DO UPDATE SET last_seen_at = @now,
                  email = COALESCE(EXCLUDED.email, app_users.email),
                  name  = COALESCE(EXCLUDED.name,  app_users.name)
  RETURNING id;
  ```
  This returns the canonical `id` whether it inserted or matched — no catch/reload/retry, and 12
  concurrent first sign-ins converge on **one** row (mirror RagLab's
  `UserDirectoryConcurrencyTests` assertion).
- `IUserResolver.ResolveCurrentUserAsync(ClaimsPrincipal) → UserId` for request handlers: read the
  `sub` claim (fallback `ClaimTypes.NameIdentifier`), read-first then `RecordAsync` on miss.
- Everything keys to our internal `UserId` (uuid), **never** the raw `sub`, so the IdP stays
  swappable.
- **`NullUserDirectory`** (graceful degradation, like `NullSessionStore`): when no DB/Keycloak is
  configured, the API still runs — anonymous only.

### 5. Durable store — associate sessions with a user

- Add nullable `user_id uuid NULL REFERENCES app_users(id)` to `conversation_sessions` (migration in
  the `PostgresSessionStore` initializer; anonymous sessions keep `user_id = NULL`).
- `ISessionStore` gains `SaveAsync(sessionId, payload, userId?)` and
  `ListForUserAsync(userId)` (most-recent-first, capped). The anonymous flow is the `userId = null`
  path — unchanged.
- `FoundryAgentGateway` write-through passes the resolved `userId` (or null) so each persisted turn
  is stamped with its owner.

### 6. Endpoints

- `GET /api/me` `RequireAuthorization()` — resolves the caller to the internal user
  `{ id, provider, sub, email, name }` (RagLab's identity contract; also the smallest end-to-end
  proof that tokens validate + resolve). Returns 401 when the token lacks a subject.
- `GET /api/copilot/history` `RequireAuthorization()` — the user's past sessions (id + first-message
  preview + timestamps), owner-scoped by `user_id`; never another user's rows.
- `GET /api/copilot/history/{sessionId}` — a specific owned conversation (403/404 if not the owner).
- `/api/copilot/ask` unchanged in shape; when authenticated, the resolved `userId` flows to the
  store so the turn joins the user's history.

### 7. Anonymous → authenticated hand-off (HomeScout-specific)

- On the **first authenticated** `/ask` that still carries an `hs_session` cookie for a
  `user_id IS NULL` session, **claim** it (set `user_id`) so the in-progress conversation survives
  login.
- Open question: does logout also `session/reset`, or continue anonymously?

## Auth + session contract (backend ↔ frontend) — enables parallel work

API-first, so Codex builds login while the backend builds validation:

- **Token:** the SPA (`homescout-web` client) does Authorization Code + PKCE against the `homescout`
  realm, then sends `Authorization: Bearer <access_token>`. The existing `hs_session` cookie stays
  for the anonymous path (`credentials: 'same-origin'` already set in `frontend/src/App.tsx`).
- **Anonymous still works** with no token — unchanged.
- **History:** `GET /api/copilot/history` (bearer) returns the list for a "your conversations" panel;
  `GET /api/me` for the signed-in identity.
- **Frontend (Codex) tasks:** login/logout UI + token acquisition & silent refresh (Keycloak JS or
  `oidc-client-ts`), attach the bearer header, a history panel, and re-open a past conversation. The
  anonymous chat keeps working with zero changes until login is added.

## Security

- Validate issuer, audience (`homescout-api`), expiry, signature (realm JWKS) — reject tokens minted
  for other clients/audiences.
- `RequireHttpsMetadata = true` outside dev; PKCE (no browser secret); never tokens in URLs.
- Owner checks on every per-user read (defence in depth beyond `WHERE user_id = @me`).
- Keep the durable store's idle/absolute expiry; authenticated history stays bounded.
- Least-privilege: the API only *validates* tokens (JWKS); it does not admin the realm.

## Testing strategy (seam-first) — model: RagLab `Drivers/TestAuthHandler.cs`, `PostgresFixture.cs`

- **Offline (PR gate):** a **`TestAuthHandler`** (a stub `AuthenticationHandler` registered via
  `ConfigureTestServices`, with `X-Test-Subject` / `X-Test-Omit-Subject` headers) exercises
  `[Authorize]` endpoints **without a live Keycloak or real signing keys**. Assert: anonymous
  `/api/copilot/ask` still 200; `/api/me` and `/history` are 401 without a subject, 200 with one,
  and history **never** returns another user's rows.
- **User directory (Category=Database):** the atomic upsert + concurrency (12 parallel first
  sign-ins → one row, one id) against a Testcontainers Postgres, mirroring
  `UserDirectoryConcurrencyTests`. Runs in the gate (like `PostgresSessionStoreTests`), self-skips
  without Docker.
- **No Testcontainers Keycloak** (RagLab doesn't either — auth is stubbed offline). Real token
  validation is proven **live** against the Aspire-run Keycloak.
- **Live (`[Category("External")]`/manual):** end-to-end sign-in → `/api/me` → per-user history →
  anonymous-claim hand-off against Aspire Keycloak — verified before calling it done
  (*verify, don't assume*).

## Phased steps (ordered; backend = me, frontend = Codex in parallel from step 2's contract)

1. ✅ **Realm + Aspire hosting** *(done + verified 2026-07-05)* — committed
   `AppHost/keycloak/homescout-realm.json` (realm `homescout`; `homescout-api` bearer-only +
   `homescout-web` public/PKCE clients with the `homescout-api` audience mapper; `dev`/`jane` test
   users, `user` role); `Aspire.Hosting.Keycloak` (`13.4.5-preview.1.26316.12`) added to the AppHost
   with its own data volume + `WithRealmImport("./keycloak")`; API references + waits for it. Offline
   `KeycloakRealmTests` lock the realm shape. **Verified** against a real Keycloak (the export mounted
   as Aspire mounts it): the realm imports, OIDC discovery + JWKS (RS256) resolve, and a
   password-grant token for `dev` carries `sub` + **`aud: homescout-api`** (audience mapper working) —
   de-risks step 2's validation.
2. **API JWT validation + `/api/me`** — `AddKeycloakJwtBearer` (audience check) + `OnTokenValidated`
   JIT capture; `GET /api/me` returns the resolved user. Offline `TestAuthHandler` tests.
3. **User directory** — `app_users` table + `IUserDirectory` atomic upsert + `NullUserDirectory`
   fallback; Testcontainers concurrency test.
4. **Store user association** — `conversation_sessions.user_id` migration; user-aware `ISessionStore`;
   gateway stamps the owner.
5. **History endpoints** — `GET /api/copilot/history[/{id}]`, owner-scoped, authorized; tests.
6. **Anonymous→authenticated hand-off** — claim an anonymous session on first authenticated ask.
7. **Frontend (Codex)** — login/logout, bearer header, history panel.
8. **Live verification** — end-to-end against Aspire Keycloak; record in the log.

## Open questions / verify-at-implementation

- **Aspire Keycloak API surface** — confirm `AddKeycloak` / `WithRealmImport` / `AddKeycloakJwtBearer`
  signatures against the pinned preview at build time (RagLab uses the exact version above).
- **Realm-export shape** — start from RagLab's `realm-export.json` and trim to HomeScout's clients.
- **Logout semantics** — does logout also reset the anonymous cookie session?
- **Token lifetime / refresh** — modest access-token lifespan + silent refresh in the SPA.
