# Quality Gate & Test Plan

The quality gate is the set of checks that must pass before any change merges. It
was stood up in Phase 1 of the
[master migration plan](../00-roadmap/homescout-skeleton-migration-plan.md) — before
any project moved — so every later phase runs inside a green gate. It is
**additive**: each phase adds checks and none may regress.

## Gate Checks

| Check | Local command | CI workflow | Since |
| --- | --- | --- | --- |
| Plan drift | `scripts/check-plan-drift.sh` | `plan-drift.yml` | Phase 1 |
| Backend build + fast tests (NUnit; incl. BDD from Phase 3) | `dotnet test --filter "Category!=Integration"` | `backend-ci.yml` | Phase 1 |
| Frontend build + lint + unit test | `npm run build && npm run lint && npm run test` | `frontend-ci.yml` | Phase 1 |
| Frontend e2e smoke | `npm run e2e` | `frontend-ci.yml` | Phase 4 |
| Backend integration (Aspire) | `dotnet test --filter "Category=Integration"` | (deferred) | Phase 4 |

Run everything locally at once with `scripts/quality-gate.sh` — it produces the
same result as CI. CI triggers on every PR and on push to `main`; a red required
check blocks merge. Configure branch protection on `main` to require `plan-drift`,
`backend-ci`, and `frontend-ci`.

## Test Layers

All backend test projects use **NUnit** (RagLab parity), so a single framework
carries plain tests and BDD.

- **Contract tests** (`ApiContractTests`, NUnit) — boot the API in-memory with
  `WebApplicationFactory` (node-free, ~sub-second) and assert the public response
  shape of `GET /api/status` and `GET /api/comparison/sample`. These are the
  **behaviour-lock**: they must keep passing *unedited* while projects move
  (Phase 2) and `.ApiService` is split into `.API`/`.API.Service` (Phase 3),
  proving the endpoints did not change.
- **Integration tests** (`WebTests`, `[Category("Integration")]`) — boot the full
  Aspire AppHost (API + Vite frontend as processes). Slower and need Node, so they
  are excluded from the fast required gate (`--filter "Category!=Integration"`) and
  run locally / in a dedicated CI job added in Phase 4.
- **BDD** (Reqnroll.NUnit + Allure, added Phase 3) — Gherkin `Features/` with
  `StepDefinitions/`, `Drivers/` (an `ApiDriver` over `WebApplicationFactory`), and
  `Hooks/`. First scenario: `Status.feature` for `GET /api/status`. `Bogus` supplies
  fake data; `Testcontainers.PostgreSql` is added only once persistence exists.
- **Unit tests** — arrive with the service layer in Phase 3 (FluentResults →
  ProblemDetails mapping, `.API.Service` handlers, deterministic tools such as the
  cost estimator).

### Frontend

- **Unit/component tests** (Vitest + Testing Library, jsdom) — `src/**/*.test.tsx`.
  The seed test asserts the workspace shell renders its core regions.
- **E2E** (Playwright) — added in Phase 4: load the workspace and confirm the key
  regions are visible.

## Rules

- Every slice ships tests for the surfaces it changes.
- No live network in the fast gate; stub `fetch` (frontend) and use in-memory hosts
  (backend). External providers get fake HTTP handlers / fixtures when they land.
- Contract tests change only when the contract intentionally changes — and then the
  owning plan and `wiki/endpoint-summary.md` change in the same commit.

See `wiki/testing-strategy.md` for the standing (non-gate) test strategy.
