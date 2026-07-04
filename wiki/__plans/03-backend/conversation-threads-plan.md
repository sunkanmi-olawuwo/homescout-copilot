# Conversation Threads Plan (Multi-Turn, Anonymous)

**Status:** Backend slice 1 **done, live-verified (2026-07-04)** — anonymous session cookie +
in-memory session registry + gateway session support + reset endpoint. **Durable PostgreSQL store
done (2026-07-05, Testcontainers-verified; live restart path pending Foundry).** Multi-turn
eval-harness cases remain; the frontend "New conversation" button shipped separately.

**Owning context:** follow-up to [[Copilot Agent Gateway — Design]]; the copilot is **single-turn
today** (each `AskAsync` is independent), so context-dependent follow-ups can't be answered.

## Goal

Real multi-turn conversation — "and on interest-only?" after a cost question keeps context — with
**no auth** (anonymous, session-scoped). Durable per-user history comes later (via **Keycloak**,
see [[Plan Divergence]]).

## Query rewrite? No — the model resolves context from history

A common assumption is that follow-ups need a **query-rewrite** step (reformulating "and on
interest-only?" into a standalone query). For HomeScout **that is not needed**:

- The Agent Framework passes the **full conversation history** to the model each turn. The model
  resolves the follow-up **natively** (coreference/context is normal LLM behaviour) and extracts the
  carried-over tool parameters (£300k / £30k / 4.5% / 25yr) + the new `repaymentType`.
- What the framework/Foundry "takes care of" is **history-passing (thread state)** — not query
  rewriting. There is no separate rewrite step.

Query rewrite becomes relevant **only when we add retrieval/RAG** (case files / knowledge base — a
later phase): a terse follow-up makes a poor semantic-search query, so it must be rewritten to be
self-contained *before* retrieval. Our tools today are deterministic functions, not search, so
there is nothing to rewrite. Revisit rewrite when RAG lands (and even then it's a targeted add-on;
Foundry's search tool only partly helps).

## Design

- **Thread state:** create/hold an `AgentThread` (Microsoft Agent Framework) per **anonymous session
  id** (opaque, server-issued cookie or client id), and pass it to `RunAsync(message, thread)` so
  turns accumulate context.
- **Store:** an in-memory thread registry first (matches the "Basic setup, Microsoft-managed
  threads" decision in [[Copilot Agent Gateway — Design]]); durable persistence (Cosmos / the
  "Standard setup") next so history survives restarts.
- **History strategy:** **full history** is fine for short homebuying Q&A. **Compaction /
  summarization** only matters for long threads — **defer** it; add when thread length is a real
  problem.
- **Gateway shape:** `AskAsync` takes a session id; the gateway resolves the session's thread (or
  starts one) and runs the agent against it. Keep the singleton `AIProjectClient` + per-request
  tool-bound agent (unchanged); the thread is the added state.
- **Auth:** anonymous session id now; associate sessions with a **Keycloak** user later for
  cross-device / per-user history.

## Session-id contract (backend ↔ frontend) — enables parallel work

The two tracks are coupled only by **how the session id flows**. Define the contract up front and
both tracks build against it in parallel (API-first).

- **Backend-managed HttpOnly cookie** — the API sets/reads a `hs_session` cookie (HttpOnly,
  `SameSite=Lax`, `Secure` in production, ~24 h Max-Age). The browser sends it automatically on
  every `POST /api/copilot/ask`, so the frontend needs **no session-management code** and the
  existing chat UI keeps working — it just gains memory.
- **Reset:** `POST /api/copilot/session/reset` clears the current session's thread and issues a
  fresh cookie (start a new conversation).
- **Request/response unchanged** otherwise — `CopilotRequest`/`CopilotAnswer` keep their shape; the
  session rides in the cookie, not the body.

**Frontend (Codex) task — small, well-scoped:** add a **"New conversation"** control that calls
`POST /api/copilot/session/reset` and clears the on-screen conversation. That is the *only* required
frontend change (the cookie is automatic). Everything else — sending messages, rendering turns —
already exists. Codex can build this in parallel against the contract above; if we ship reset in a
later slice, the frontend needs nothing at all for multi-turn to work.

**Parallel split:** backend (me) — cookie + `AgentThread` registry + reset endpoint; frontend
(Codex) — the reset button; **E2E check** (both merged) — verify "and on interest-only?" carries
context end to end.

## Session expiry (defaults — tunable via config)

| Layer | Default | Rationale |
| --- | --- | --- |
| Server thread — **idle** | **60-min sliding** (reset each turn; GC threads idle > 60 min) | Forgiving for research breaks; bounds memory |
| Server thread — **absolute cap** | **~24 h** | Anonymous sessions shouldn't live forever |
| Cookie | HttpOnly, `SameSite=Lax`, `Secure` (prod), **~24 h** Max-Age | Survives tab reloads; not JS-accessible |
| Durable store (Cosmos, later) | **TTL ~24 h** | Native Cosmos TTL evicts stale anonymous threads |

Anonymous conversations can contain the buyer's own figures (price, deposit), so we don't hoard
them: 60-min idle / 24-h cap balances continuity against not lingering mildly-sensitive data. In the
first (in-memory) cut, an API restart clears everything, so the idle GC mainly bounds memory.

## Verification — add multi-turn eval cases

Our dataset already has the canonical follow-up, `cost-interest-only` ("And on interest-only?"), but
it is currently evaluated **standalone** (single-turn), so it doesn't actually test context-carry —
the copilot can't answer it well today. When threads land:

- Add **multi-turn eval cases** (an ordered turn sequence per case) that assert the second turn
  reuses the first turn's figures — the proof that "no rewrite needed" holds for our tools.
- Extend the harness to drive a thread across turns (not one independent query per row), and check
  the follow-up answer contains the carried-over estimate.

## Phased steps

Backend (me) and frontend (Codex) run in parallel against the contract above.

1. ✅ **Session cookie** — the HttpOnly `hs_session` cookie is issued/read in `CopilotEndpoints`
   (`ResolveSession`), expiry per the defaults above.
2. ✅ **Session registry (in-memory)** — `ConversationSessionRegistry` (singleton): session id →
   `AgentSession`; `FoundryAgentGateway` runs the turn against it (null session id = stateless
   single-turn, unchanged); `ConversationSessionSweeper` (`BackgroundService`) evicts idle/expired.
   Uses the Agent Framework's **`AgentSession`** (created via `agent.CreateSessionAsync`, run via
   `RunAsync(message, session, …)`, serializable for the durable store later — the SDK renamed the
   thread concept to *session*).
3. ✅ **Reset endpoint** — `POST /api/copilot/session/reset` drops the session + clears the cookie.
   *(Frontend, parallel: the "New conversation" button — not yet built.)*
   - **Live-verified 2026-07-04** (`FoundryAgentGatewayLiveTests`): a follow-up "And on
     interest-only?" with **no figures restated** returned the correct interest-only estimate
     (£1,012.50) — across **two gateway instances sharing one registry** (the production scoped
     shape), proving `AgentSession` carries context cross-instance. Registry logic covered offline
     (`ConversationSessionRegistryTests`).
4. **Multi-turn eval cases** *(next)* — ordered turns; harness drives a session; assert context
   carries (the live gateway test already proves the mechanism).
5. ✅ **Durable store — PostgreSQL** *(done 2026-07-05; Testcontainers-verified, live path pending
   Foundry)* — `ISessionStore` seam with `PostgresSessionStore` (real) + `NullSessionStore`
   (graceful "durability off" default). `FoundryAgentGateway` is now write-through: on a session
   miss the in-memory registry rehydrates from the store via
   `AIAgent.DeserializeSessionAsync(JsonElement)`, and after each turn the session is persisted via
   `AIAgent.SerializeSessionAsync` (skipped when the store isn't persistent). Table
   `conversation_sessions(session_id PK, payload jsonb, created_at, last_active_at)` created by a
   startup initializer; `ConversationSessionSweeper` also `DELETE`s idle/absolute-expired rows;
   reset purges the store too. Wired in Aspire (`AddPostgres("postgres").AddDatabase("sessions")`),
   config-gated in `Program.cs` (no connection string → `NullSessionStore`, in-memory only as
   before).
   - **Tested:** `PostgresSessionStoreTests` (Testcontainers, `Category=Database` — runs in the PR
     gate, self-skips without Docker) cover save/load round-trip, upsert, remove, and sweep;
     `NullSessionStoreTests` + the reset-endpoint test cover the no-op path and store purge. The
     full deserialize-into-a-live-agent path has a `[Category("External")]` restart test
     (`Copilot_recovers_a_session_from_the_durable_store_across_a_restart`) that runs when Foundry
     is provisioned — **pending live verification** (not yet run against a real Foundry project).
   - **Decision (2026-07-04): use PostgreSQL, not Cosmos, for *our* durable store.** The store
     only needs to key a serialized session blob by session id with an expiry — a single
     `conversation_sessions(session_id PK, payload jsonb, created_at, last_active_at)`
     row per session. Postgres fits this exactly, Aspire has first-class Postgres
     support (local container in dev → Azure Database for PostgreSQL in prod), it is far cheaper
     than a Standard-setup Cosmos (≥3000 RU/s), and **Keycloak already runs on Postgres** — so the
     session store and identity share one engine. TTL: Cosmos has native TTL, but we already run
     `ConversationSessionSweeper`, which `DELETE`s idle/absolute-expired rows — no capability lost.
   - **Not the same Cosmos as the Foundry *Standard* capability host.** That Cosmos (+ AI Search +
     Storage) is Foundry's *server-side* thread storage and is a separate, deferred platform
     decision (we're on **Basic**/Microsoft-managed today). Persisting the *serialized* session
     client-side in Postgres is exactly the Basic-compatible path and keeps us off the Standard
     bundle. See [[Plan Divergence]] for the Basic-vs-Standard record.
6. (Later) **Keycloak** identity → per-user history — associate persisted sessions with a Keycloak
   `sub` so history is per-user and cross-device (depends on step 5). Also **compaction** if threads
   grow long; **query rewrite** only if/when RAG retrieval is added.

## Open questions / verify-at-implementation

- Exact `AgentThread` persistence/serialization surface for the durable store (service-backed
  Foundry thread id vs serialized messages) — confirm against the SDK at implementation time.
