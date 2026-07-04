# Conversation Threads Plan (Multi-Turn, Anonymous)

**Status:** Design-first, queued (top of the backend queue). Not yet implemented.

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

1. **Session cookie** — issue/read the HttpOnly `hs_session` cookie at the API boundary (expiry
   defaults above).
2. **Thread registry (in-memory)** — session id → `AgentThread`; gateway runs against it; idle GC.
3. **Reset endpoint** — `POST /api/copilot/session/reset`. *(Frontend, parallel: "New conversation"
   button.)*
4. **Multi-turn eval cases** — ordered turns; harness drives a thread; assert context carries.
5. **Durable store** — persist thread state (Cosmos / Standard setup) so history survives restarts.
6. (Later) **Keycloak** identity → per-user history; **compaction** if threads grow long; **query
   rewrite** only if/when RAG retrieval is added.

## Open questions / verify-at-implementation

- Exact `AgentThread` persistence/serialization surface for the durable store (service-backed
  Foundry thread id vs serialized messages) — confirm against the SDK at implementation time.
