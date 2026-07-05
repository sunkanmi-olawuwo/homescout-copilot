# Codex Frontend Instructions — Sign-In + Per-User History (Keycloak)

*Handoff for Codex (the frontend agent). This is **step 7** of
[Keycloak Auth + Per-User History](../03-backend/keycloak-auth-plan.md). The backend (steps 1–6) is
**done and live-verified**; this page is the precise API + OIDC contract to build the frontend
against. Read [Codex Frontend Instructions — Build From The Claude Design](./codex-frontend-instructions.md)
first for the design system, tokens, and how to work with the Claude Design source.*

## Goal

Add **end-user sign-in** to the existing React app so a signed-in buyer/renter sees **their**
conversation history, while the **anonymous copilot keeps working unchanged**. Auth is *additive* —
do not gate the existing chat behind login.

## What to build (scope)

1. **Sign in / sign out** (OIDC Authorization Code + PKCE against Keycloak).
2. **Attach the access token** as `Authorization: Bearer <token>` to API calls when signed in.
3. **Signed-in identity** in the header (name/email + a sign-out control), from `GET /api/me`.
4. **History panel** — the user's past conversations from `GET /api/copilot/history`.
5. **Anonymous continuity** — a visitor can chat anonymously, then sign in and keep going (the
   backend claims the in-flight session automatically; see below).

## The API contract (ready — do not change the backend)

All under the same origin; the existing `hs_session` **HttpOnly cookie** stays (`credentials:
'same-origin'`) for the anonymous path. Add the bearer header **when signed in**.

| Endpoint | Method | Auth | Returns |
| --- | --- | --- | --- |
| `/api/copilot/ask` | POST | **optional** bearer | `CopilotAnswer` (unchanged). With a token, the turn is owner-stamped for history. Without, anonymous as today. |
| `/api/me` | GET | **bearer required** | `{ userId, subject, email, name }` — the signed-in identity (401 without a valid token). |
| `/api/copilot/history` | GET | **bearer required** | `{ conversations: [{ sessionId, createdAt, lastActiveAt }] }` — most-recent-first. Empty array if none. |
| `/api/copilot/history/{sessionId}` | GET | **bearer required** | `ConversationSummary`, or **404** if not owned. |
| `/api/copilot/session/reset` | POST | none | 204 — "New conversation" (already wired). |

Notes:
- **Send the bearer header on every authenticated call** (`/api/me`, `/api/copilot/history*`, and
  `/api/copilot/ask` when signed in). Anonymous calls send no token.
- `ConversationSummary` is **metadata only** (id + timestamps) — no message preview yet (a backend
  enhancement can add a title later).

## OIDC / Keycloak config

- **Realm:** `homescout`. **Client:** `homescout-web` — public SPA, **Authorization Code + PKCE
  (S256)**, no secret. **Audience/scope:** request the token for the API (`homescout-api` audience is
  stamped by the realm mapper).
- **Redirect URIs already allowed** in the committed realm
  (`dotnet/src/HomeScoutCopilot.AppHost/keycloak/homescout-realm.json`): `http://localhost:5173/*`,
  `https://localhost:5173/*`, `http://localhost:4173/*`. Add the deployed origin to the realm export
  (backend change) when we deploy.
- **Test users** (dev only, in the realm): `dev`/`dev`, `jane`/`dev`.
- **Library:** add **`oidc-client-ts`** + **`react-oidc-context`** (standard React OIDC, PKCE
  built-in, IdP-agnostic — keeps us aligned with the "internal id, swappable IdP" backend design).
  `keycloak-js` is an acceptable alternative if you prefer the vendor SDK.
- **Config via Vite env** (`frontend/.env`): `VITE_OIDC_AUTHORITY` (the Keycloak realm issuer, e.g.
  `http://localhost:8080/realms/homescout`), `VITE_OIDC_CLIENT_ID=homescout-web`, and the API scope.
  Store the token **in memory** (react-oidc-context default) — not localStorage — and use **silent
  renew** for refresh.
- **Access-token lifetime** is 15 min in the realm; rely on silent renew.

## Two things to confirm / flag back to the backend (me)

1. **Surfacing the Keycloak URL to the SPA.** Aspire injects the Keycloak endpoint into the *API*,
   not the Vite app. For dev, set `VITE_OIDC_AUTHORITY` from the Aspire-assigned Keycloak URL (or we
   add a tiny unauthenticated `GET /api/config` that returns `{ authority, clientId }` for the SPA to
   read at startup — tell me if you'd prefer that; it's a small backend add).
2. **Re-opening a past conversation.** `hs_session` is an **HttpOnly cookie the SPA can't set**, so
   clicking a history item can't resume that thread yet. For the first cut, render history as a
   **read-only list** (proof of per-user history). Full "re-open" needs a small backend companion —
   `POST /api/copilot/session/resume/{sessionId}` (owner-checked, sets the `hs_session` cookie). Ask
   me to build it when you want re-open wired.

## Anonymous → authenticated continuity (already handled by the backend)

You don't need to migrate anything: when a signed-in user sends the next `/api/copilot/ask` with the
bearer header, the backend **claims** the current anonymous `hs_session` for that user (proven live).
So after sign-in, the ongoing conversation simply becomes theirs and shows up in history.

- **Logout semantics — your product call:** on sign-out, either keep chatting anonymously (leave the
  cookie) or call `POST /api/copilot/session/reset` to start fresh. Recommend the latter for a clean
  "you're signed out" state; confirm the UX with the design.

## Design & placement

Follow [Frontend Design Guidelines](../../frontend-design-guidelines.md) and the Claude Design source
(see the sibling instructions). Fit the new UI into the existing shell:

- **Header (navy):** a **Sign in** button when anonymous; the user's name/email + **Sign out** when
  authenticated. Reuse the existing header action styling.
- **Left sidebar:** a **"Your conversations"** section (the history list) under the existing *Saved
  comparisons* — visible only when signed in. Each row shows a date; clicking is a no-op until the
  resume endpoint lands (or navigate/annotate per the design).
- Keep the not-mortgage/tenancy-advice and provenance conventions intact.

## Testing

- **Component tests (Vitest):** the signed-in vs anonymous header states; the history list renders
  from a mocked `/api/copilot/history`; the API client attaches the bearer header only when signed in.
- **E2E (Playwright):** the anonymous chat still works with no login (guard against regressions). Full
  sign-in E2E needs a running Keycloak, so it runs against the Aspire stack, not the offline gate —
  keep it out of the blocking suite (tag/skip when Keycloak isn't reachable), mirroring the backend's
  live-test discipline.

## Definition of done

- Anonymous chat unchanged; sign-in/out works against the `homescout` realm; `/api/me` renders the
  identity; the history panel lists the signed-in user's conversations; the bearer header is attached
  on authenticated calls; component tests green; the anonymous-still-works E2E green.
