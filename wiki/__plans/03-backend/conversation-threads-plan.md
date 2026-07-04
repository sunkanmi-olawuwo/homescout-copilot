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

## Verification — add multi-turn eval cases

Our dataset already has the canonical follow-up, `cost-interest-only` ("And on interest-only?"), but
it is currently evaluated **standalone** (single-turn), so it doesn't actually test context-carry —
the copilot can't answer it well today. When threads land:

- Add **multi-turn eval cases** (an ordered turn sequence per case) that assert the second turn
  reuses the first turn's figures — the proof that "no rewrite needed" holds for our tools.
- Extend the harness to drive a thread across turns (not one independent query per row), and check
  the follow-up answer contains the carried-over estimate.

## Phased steps

1. **Session id** — issue/read an anonymous session id (cookie) at the API boundary.
2. **Thread registry (in-memory)** — session id → `AgentThread`; gateway runs against it.
3. **Multi-turn eval cases** — ordered turns; harness drives a thread; assert context carries.
4. **Durable store** — persist thread state (Cosmos / Standard setup) so history survives restarts.
5. (Later) **Keycloak** identity → per-user history; **compaction** if threads grow long; **query
   rewrite** only if/when RAG retrieval is added.

## Open questions / verify-at-implementation

- Exact `AgentThread` persistence/serialization surface for the durable store (service-backed
  Foundry thread id vs serialized messages) — confirm against the SDK at implementation time.
- Session-id issuance + expiry policy (cookie lifetime, anonymous-session GC).
