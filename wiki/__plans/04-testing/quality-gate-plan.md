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
| Frontend build + lint + unit test | `pnpm run build && pnpm run lint && pnpm run test` | `frontend-ci.yml` | Phase 1 |
| Frontend e2e smoke | `pnpm run e2e` | `frontend-ci.yml` | Phase 4 |
| Backend integration (Aspire) | `dotnet test --filter "Category=Integration"` | (deferred) | Phase 4 |
| External dependency checks (live BoE base rate) | `dotnet test --filter "Category=External"` | `external-checks.yml` (nightly + on demand, **non-blocking**) | — |

Run everything locally at once with `scripts/quality-gate.sh` — it produces the
same result as CI. CI triggers on every PR and on push to `main`; a red required
check blocks merge. Configure branch protection on `main` to require `plan-drift`,
`backend-ci`, and `frontend-ci`.

**External dependencies are verified, not assumed.** Tests that make real
third-party calls carry `[Category("External")]` and are kept out of the PR gate (a
provider outage must not block merges). The `external-checks.yml` workflow runs them
on a schedule so we find out promptly if a source starts blocking us or changes its
format. The app also degrades gracefully (resilient fallback) and reports which path
served a value (`Live`/`Cache`/`Fallback` provenance) so production is observable.

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
- **External/live tests** (`BaseRateLiveTests`, `[Category("Integration")]` +
  `[Category("External")]`) — make a real call through the wired app (e.g. the live
  Bank of England base-rate fetch) and assert a non-fallback (`Live`) result. Excluded
  from the PR gate; run by `external-checks.yml` nightly. Verified working
  end-to-end; a `Fallback` result here is the signal that the live path has broken.
- **BDD** (Reqnroll.NUnit + Allure) — Gherkin `Features/` with `StepDefinitions/`
  and `Drivers/` (an `ApiDriver` over `WebApplicationFactory` + the typed
  `HomeScoutApiClient`). First scenario: `Status.feature` for `GET /api/status`.
  Allure writes results under the test `bin/` (gitignored). `Bogus` and
  `Testcontainers.PostgreSql` are added only when a scenario needs fake data / a DB.
- **Unit tests** — arrive with the service layer in Phase 3 (FluentResults →
  ProblemDetails mapping, `.API.Service` handlers, deterministic tools such as the
  cost estimator).

### Frontend

- **Unit/component tests** (Vitest + Testing Library, jsdom) — `src/**/*.test.tsx`.
  The seed test asserts the workspace shell renders its core regions.
- **E2E** (Playwright, chromium) — `frontend/e2e/`, run against the built app via
  `vite preview` (no API; the workspace uses its fallback). Smoke: load the
  workspace and confirm the core regions are visible. Runs in `quality-gate.sh` and
  as a required step in `frontend-ci.yml`.

## Rules

- Every slice ships tests for the surfaces it changes.
- No live network in the fast gate; stub `fetch` (frontend) and use in-memory hosts
  (backend). External providers get fake HTTP handlers / fixtures when they land.
- Contract tests change only when the contract intentionally changes — and then the
  owning plan and `wiki/endpoint-summary.md` change in the same commit.

See `wiki/testing-strategy.md` for the standing (non-gate) test strategy.
