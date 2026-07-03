# HomeScout Skeleton Migration — Master Sequenced Plan

This is the **source of truth** for restructuring HomeScout Copilot to mirror the
RagLab (`HBK.Insights.Raglab`) project skeleton. Execute phases in order. Each
phase has exit criteria; do not start a phase before its dependency is met.

Reference skeleton: `/Users/olaheavy/source/code/netcore/HBK.Insights.Raglab`.

## Decisions (locked)

- **Scope:** full clone including backend layering (RagLab's `.API` / `.API.Service`
  / `.API.Client` / `.Shared` / `.Functional` split).
- **Frontend location:** stays at repo root `frontend/` (intentional divergence
  from RagLab, which nests Web under `dotnet/src/`). Record in Plan Divergence.
- **Result type:** third-party **FluentResults** instead of RagLab's custom
  `HBK.Insights.Functional.Result`. HomeScout's `.Functional` project is a thin
  extensions layer over FluentResults (typed `Error`/`Success` reasons +
  HTTP/ProblemDetails mappers), not a new monad.
- **Test framework:** **NUnit** across all test projects (RagLab parity), with
  **Reqnroll** for BDD/Gherkin and **Allure** reporting introduced in Phase 3. Do
  not mix xUnit into the test assemblies.
- **No premature scaffolding.** RagLab's `dotnet/poc/`, `dotnet/infra/`, and
  `scripts/byo/` are *not* mirrored as empty folders. Infrastructure (`infra/`,
  `scripts/byo/`) is added only when Azure deployment work begins (Phase 7 of the
  [phased learning and build plan](./phased-learning-build-plan.md)); an experiment
  sandbox (`poc/`) is added only if/when we actually spike retrieval/agent work. The
  course companion (`rwjdk/chatbot`) stays an external reference, not vendored.
- **Frontend package manager:** **pnpm** (RagLab parity). The version is pinned via
  `packageManager` in `frontend/package.json`; pnpm settings (build allowlist,
  overrides) live in `frontend/pnpm-workspace.yaml`; Aspire's `AddViteApp` uses
  `.WithPnpm()`.

## Target Structure

```text
home-scout-pilot/
├─ AGENTS.md + CLAUDE.md (symlink)     # RagLab-style: plan-sync protocol + invariants
├─ nuget.config                         # renamed from NuGet.Config
├─ aspire.config.json
├─ frontend/                            # React/Vite stays at root (+ Vitest, Playwright e2e)
├─ scripts/
│  ├─ check-plan-drift.sh               # CI-enforced drift check (HomeScout invariants)
│  └─ quality-gate.sh                   # runs drift + backend + frontend locally
├─ .github/
│  ├─ copilot-instructions.md           # points to AGENTS.md
│  └─ workflows/                        # backend-ci.yml, frontend-ci.yml, plan-drift.yml
├─ dotnet/
│  ├─ HomeScoutCopilot.slnx             # .slnx with /src and /tests solution folders
│  ├─ src/
│  │  ├─ HomeScoutCopilot.AppHost
│  │  ├─ HomeScoutCopilot.ServiceDefaults
│  │  ├─ HomeScoutCopilot.API           # minimal-API host + endpoint definitions
│  │  ├─ HomeScoutCopilot.API.Service   # application/service layer (agent gateway, tools)
│  │  ├─ HomeScoutCopilot.API.Client    # typed HTTP client for Web + tests
│  │  ├─ HomeScoutCopilot.Shared  # DTOs, contracts, shared app types
│  │  └─ HomeScoutCopilot.Functional    # FluentResults-based Result helpers + mappers
│  └─ tests/
│     ├─ HomeScoutCopilot.API.Test
│     ├─ HomeScoutCopilot.Shared.Test
│     └─ HomeScoutCopilot.Functional.Test
└─ wiki/
   ├─ index.md, log.md, overview.md, coding-conventions.md, component-architecture.md,
   │  backend-architecture.md, endpoint-summary.md, feature-coverage.md,
   │  testing-strategy.md, poc-to-product.md, ...
   ├─ raw/
   └─ __plans/                          # renamed from plan/, numbered phase folders
      ├─ README.md                      # index + "Detecting Plan Drift"
      ├─ 00-roadmap/                    # this file + existing roadmap plans
      ├─ 01-design/
      ├─ 02-frontend/
      ├─ 03-backend/
      └─ 04-testing/
```

## Non-negotiable Invariants (to be enforced by the drift check + CI)

- **API-first.** The React frontend calls `HomeScoutCopilot.API`; it never owns
  agent orchestration. Agent work sits behind `.API.Service`.
- **Microsoft Foundry Agent Service** is the target agent platform; no classic
  Foundry agents for new work.
- **Not mortgage advice.** No regulated mortgage-product recommendation; no
  simplistic safe/unsafe area label; estimates always ship with assumptions.
- **Expected failures use FluentResults**, not exceptions, across `.API.Service`.
- **Every slice ships tests** for the surfaces it changes.
- **Plans and code do not drift**; names (routes, DTOs, tools) stay identical
  between the owning plan and the code.

## Quality Gate (starts Phase 1)

The quality gate is stood up in Phase 1 — before any project moves — so every
later phase is protected. It is **additive**: each phase adds checks and none may
regress. All checks must be green before a change merges.

The gate has four required checks, run locally via one command and enforced in CI:

| Check | Local command | CI workflow | From phase |
| --- | --- | --- | --- |
| Plan drift | `scripts/check-plan-drift.sh` | `plan-drift.yml` | 1 |
| Backend build + test | `dotnet test` (sln/slnx) | `backend-ci.yml` | 1 |
| Frontend build + lint + unit test | `pnpm run build && pnpm run lint && pnpm run test` | `frontend-ci.yml` | 1 |
| Frontend e2e smoke | `pnpm run e2e` | `frontend-ci.yml` | 4 |

- A single local pre-flight script (`scripts/quality-gate.sh`) runs all
  currently-active checks so contributors get the same result as CI.
- CI triggers on every PR and on push to `main`; a red check blocks merge.
- The detailed test matrix lives in
  [Quality Gate & Test Plan](../04-testing/quality-gate-plan.md) (created in
  Phase 1).

## Branching & Merge Workflow

Every phase is delivered on its own branch and merged into `main` through a pull
request. Nothing is committed directly to `main`.

1. **Branch per phase**, cut from up-to-date `main`:
   - `migration/phase-0-plan-foundation`
   - `migration/phase-1-quality-gate`
   - `migration/phase-2-relocation`
   - `migration/phase-3-layering`
   - `migration/phase-4-e2e`
2. **Small commits** within the branch; each commit should build.
3. **Open a PR** for the phase. The PR description lists the phase's acceptance
   criteria as a checklist.
4. **Quality gate must be green** — the required CI checks (`plan-drift`,
   `backend-ci`, `frontend-ci`) block the merge until they pass.
5. **Merge to `main`** only when every acceptance criterion is verified and CI is
   green; then log the phase in `wiki/log.md` and mark it done in this plan.
6. The **next phase branches from the merged `main`**, so phases stack cleanly and
   each starts from a green baseline.

Commit messages follow the repo `AGENTS.md` attribution rule: **no co-author or
"generated by" trailers**. Branch protection on `main` should require the three
checks once the workflows exist (Phase 1).

## Phase Sequence

Each phase lists **Steps**, **Acceptance criteria** (observable outcome), and
**Verify** (exact commands / CI job that proves it). A phase is done only when
every acceptance criterion is verified green.

### Phase 0 — Plan foundation ✅ done (PR #2)
- **Steps:**
  - Author this master plan. ✅
  - Stand up `wiki/__plans/` with `00-roadmap … 04-testing`.
  - Migrate existing `wiki/plan/` pages into the numbered folders; update the
    README index and all `[[links]]` and `AGENTS.md`/`CLAUDE.md` path references.
- **Acceptance criteria:**
  - `wiki/__plans/README.md` indexes every plan file.
  - No active doc or instruction references the old `wiki/plan/` path (historical
    `wiki/log.md` entries excepted).
  - Every `[[wikilink]]` resolves to an existing page.
- **Verify:**
  - `test -f wiki/__plans/README.md`
  - `grep -rn "wiki/plan/" --include='*.md' wiki AGENTS.md README.md | grep -v 'wiki/log.md'` → no output
  - Wikilink integrity grep (same check used in the review) → all resolve.

### Phase 1 — Quality gate + governance (no project moves) ✅ done (PR #3)
- **Steps:**
  - Rewrite `AGENTS.md` in RagLab style (Plan-Sync Protocol + invariants above +
    drift-check reference). Keep `CLAUDE.md` symlink.
  - Add `scripts/check-plan-drift.sh` (POSIX grep; runs on macOS + CI) enforcing
    the HomeScout invariants; source of truth = this master plan.
  - Add `scripts/quality-gate.sh` running every active check.
  - Add `.github/workflows/` (`backend-ci`, `frontend-ci`, `plan-drift`) and
    `.github/copilot-instructions.md` → AGENTS.md.
  - **Seed the first real tests so the gate has teeth immediately:**
    - Backend: switch `HomeScoutCopilot.Tests` to **NUnit** and add an in-memory
      `WebApplicationFactory` contract test asserting `GET /api/status` and
      `GET /api/comparison/sample` status + JSON shape. (Reqnroll BDD is added in
      Phase 3; NUnit here is the shared framework.)
    - Frontend: add Vitest + Testing Library; a smoke test that `App` renders the
      workspace regions (sidebar, comparison composer, evidence panel); add
      `test` and `lint` package.json scripts.
  - Create `wiki/__plans/04-testing/quality-gate-plan.md` (test matrix + gate).
  - Rename `NuGet.Config` → `nuget.config`.
- **Acceptance criteria:**
  - Drift check reports **0 fail**.
  - Backend tests (incl. the new contract test) pass.
  - Frontend build, lint, and unit tests pass.
  - All three CI workflows run on a PR and are green; a deliberately broken check
    blocks merge.
- **Verify:**
  - `bash scripts/check-plan-drift.sh` → exit 0
  - `dotnet test HomeScoutCopilot.sln`
  - `cd frontend && pnpm run build && pnpm run lint && pnpm run test`
  - `bash scripts/quality-gate.sh` → all green
  - First PR shows the three required checks passing in GitHub Actions.

### Phase 2 — Directory relocation (mechanical; gate stays green) ✅ done (PR #4)
- **Steps:**
  - Create `dotnet/`; move `HomeScoutCopilot.AppHost`, `.ServiceDefaults`,
    `.ApiService`, and the test project under `dotnet/src` and `dotnet/tests`
    (prefer `git mv` to preserve history).
  - Convert `HomeScoutCopilot.sln` → `dotnet/HomeScoutCopilot.slnx` with `/src`
    and `/tests` folders. Fix `..` project reference paths and `AppHost`'s Vite
    path to the root `frontend/`.
  - Update `backend-ci.yml` to the `.slnx`; `quality-gate.sh` auto-detects it;
    `check-plan-drift.sh` scans repo-wide (no path change).
- **Acceptance criteria:**
  - Solution builds from the new location; all Phase 1 tests still pass unchanged.
  - Frontend build still passes; Aspire AppHost still launches API + frontend.
  - Drift check 0 fail; all CI workflows green against new paths.
- **Verify:**
  - `dotnet build dotnet/HomeScoutCopilot.slnx`
  - `dotnet test dotnet/HomeScoutCopilot.slnx`
  - `cd frontend && pnpm run build`
  - `bash scripts/quality-gate.sh` → all green

### Phase 3 — Backend layering (refactor; behaviour unchanged) ✅ done (PR #5)
- **Steps:**
  - Add `HomeScoutCopilot.Functional` (FluentResults + ProblemDetails mappers).
  - Add `HomeScoutCopilot.Shared` (DTOs/contracts).
  - Split `.ApiService` into `.API` (host + endpoints) and `.API.Service`
    (application layer / agent gateway / tools). Move `/api/status` and
    `/api/comparison/sample` into the new shape **unchanged**.
  - Add `.API.Client` (typed client); wire the frontend/tests to it as chosen.
  - Split tests into `.API.Test`, `.Shared.Test`, `.Functional.Test`
    (all NUnit); add unit tests for the FluentResults→ProblemDetails mapper and for
    one `.API.Service` handler.
  - **Reqnroll BDD (RagLab parity)** in `.API.Test`: add `Reqnroll.NUnit` +
    `Allure.Reqnroll`, a `Features/` Gherkin suite with a first `Status.feature`
    scenario for `GET /api/status`, `StepDefinitions/`, `Drivers/` (an `ApiDriver`
    over `WebApplicationFactory`), and `Hooks/` (report + API log). Add `Bogus` for
    fake data; defer `Testcontainers.PostgreSql` until persistence exists.
- **Acceptance criteria:**
  - Solution builds; the Phase 1 contract tests pass **without edits** (endpoints
    behave identically).
  - Each new project has ≥1 test; the result→ProblemDetails mapping is covered.
  - The first `.feature` scenario passes and Allure results are produced.
  - Drift check 0 fail; CI green.
- **Verify:**
  - `dotnet test dotnet/HomeScoutCopilot.slnx`
  - Contract tests for `/api/status` + `/api/comparison/sample` still pass.
  - The `Status.feature` BDD scenario passes; `ls allure-results` is non-empty.
  - `bash scripts/quality-gate.sh` → all green

### Phase 4 — End-to-end + coverage broadening — delivered, pending merge
- **Steps:**
  - Add Playwright to `frontend/` with a smoke e2e (load workspace, key regions
    visible) and add `e2e` to `frontend-ci.yml`.
  - Broaden component tests for the comparison composer and evidence panel.
- **Acceptance criteria:**
  - `pnpm run test` (unit/component) and `pnpm run e2e` (Playwright smoke) pass
    locally and in CI.
  - Frontend e2e is a required check in `frontend-ci.yml`.
- **Verify:**
  - `cd frontend && pnpm run test && pnpm run e2e`
  - CI shows the e2e job green and required.

## Open Decisions

None — all migration decisions are resolved (see Decisions (locked)).

## How To Update This Plan

- When a phase's acceptance criteria are all verified green, mark it here and log
  it in `wiki/log.md` with the verification output.
- If code needs a name this plan doesn't have (or vice versa), change both in the
  same commit. Never let the plan and code diverge.
- Record any intentional divergence from the RagLab skeleton in Plan Divergence.
