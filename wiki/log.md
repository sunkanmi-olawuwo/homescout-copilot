# Log

## 2026-07-04

### Prompt v2 — Structured, Readable Copilot Answers

- After the user flagged the copilot's wall-of-text answers, agreed the approach (markdown-driven
  structure + collapse the conversation hero) and built the backend half: **prompt v2**
  (`homescout.v2.md`, `AgentPrompt.Version` → `v2`) instructs the agent to answer in structured
  Markdown (bold headline → `##` sub-headings + bullets) and to lean on the evidence panel for
  figures rather than re-listing them. All guardrail phrases kept.
- Live-verified: a real v2 answer renders as `**headline**` + `## Assumptions` / `## Context` /
  `## Next steps` sections; `evaluator run` stayed **6/6** against the live agent. Regenerated
  the agent manifest to `homescout.v2.md`; v1 kept as history; `AgentPromptTests` → v2.
- Handed the frontend half to Codex (collapse the hero to a compact header once a conversation
  is active; render `answer.text` as sanitized Markdown) — see the Codex handoff "Third slice".

### Evaluator Live Mode — Safety Checks Against The Real Agent

- Added `evaluator run [--data <path>]`: asks the **live** copilot each dataset *query* and runs
  the safety evaluators over the **real** answers (previously they only ran over authored
  example responses). `CopilotGatewayFactory` builds the real Foundry gateway from
  `AZURE_FOUNDRY_*` (mirroring the API's DI); `LiveEvaluation` turns queries → live cases
  (model prose + caveats); the existing runner + evaluators do the rest.
- Verified live: `evaluator run` asked gpt-5-mini all 6 dataset questions → **6/6 pass** all
  guardrails; the new `EvaluatorLiveTests` (`[Category("External")]`, off the blocking gate)
  passed against the provisioned agent. `run` with no Foundry env prints a clear message + exits.
- Closes the gap flagged earlier: the safety check now runs against the live agent, not just
  curated examples. Model-graded Foundry cloud evals remain the next step.

### Backend Iteration 2 — Evaluator Harness (Safety Evals)

- Built `HomeScoutCopilot.Evaluator` (new `dotnet/tools/` project) so the now-live copilot is
  **measured**, not just working. `evaluator safety [--data <path>]` runs deterministic
  HomeScout guardrail evaluators — not-mortgage-advice disclaimer present, no mortgage-product
  recommendation, no safe/unsafe area verdict — over a version-controlled eval dataset
  (`data/homescout-eval.jsonl`, 6 curated scenarios), scoring pass rates + failures (exit 1 on
  any).
- Tests (7): each evaluator passes a compliant answer + catches its violation; the runner
  summary flags failures; a **drift guard** asserts the committed dataset stays compliant.
  Tuned the product-recommendation regex to catch multi-word product names while letting
  "recommend … a mortgage adviser" through. Tool run green (6/6); full fast suite green.
- Queued next (planned in [[Work Tracks]]): the **Foundry cloud evals** (model-graded
  intent/relevance/groundedness over live copilot responses), **AgentOps `CreateAgentVersion`**
  live-deploy, and the **area-comparison endpoint**. Also made the work-tracks branch rule
  explicit — each agent works on its own branch, never `main`.
### Frontend Iteration 2 — Copilot Answers + Evidence Panel

- Implemented the second frontend copilot slice on `feature/fe-copilot-evidence`: the composer
  and START WITH cards now post to `POST /api/copilot/ask`, render `CopilotAnswer.Text`,
  `ToolCalls`, assumptions and caveats in the conversation, and retain the 503 fallback when
  Foundry is not configured.
- Populated the right-rail Evidence tab from `CopilotAnswer.Evidence`, using the contract's
  lowercase `kind` chip, Live/Cache/Fallback provenance badge and source field.
- Verified with mocked contract responses in Vitest and Playwright; `pnpm run lint`,
  `pnpm run test`, `pnpm run build`, and `pnpm run e2e` are green.

### Backend Iteration 2 — Copilot Evidence Contract (Live-Verified)

- Built the structured evidence seam (unblocks Codex's Evidence panel): `FigureKind` +
  `EvidenceItem` + a new `CopilotAnswer.Evidence` field in `Shared/Contracts`;
  `CopilotEvidenceBuilder` maps each tool result → tagged, sourced items (estimate → monthly
  payment / LTV as `Estimate` "Live"; base rate → `Fact` with its Live/Cache/Fallback
  provenance), wired into `FoundryAgentGateway`.
- **Wire format:** `FigureKind` serialises as lowercase strings (`"estimate"` etc.) via
  `[JsonStringEnumMemberName]` — exactly what the frontend chips expect, without touching
  `RepaymentType`. Gave `HomeScoutApiClient` (and the endpoint test) a `JsonStringEnumConverter`
  so .NET consumers round-trip the string enums.
- Verified offline (`CopilotEvidenceBuilderTests` + the copilot endpoint test asserting the
  evidence trail) **and live** — `FoundryAgentGatewayLiveTests` now asserts the real agent's
  `estimate_mortgage` result becomes an `Estimate` evidence item (green against the provisioned
  project). Updated the copilot plan, work-tracks, and the Codex handoff (second slice).

### Foundry Provisioned + Copilot Live-Verified 🎉

- Ran `azd provision` into subscription **HomeScoutPilot** (eastus2): resource group, AIServices
  account, chat model deployment, Foundry project, RBAC. Fixed three real issues found by
  provisioning against the live platform:
  1. **Model deprecated** — pinned `gpt-4.1-mini 2025-04-14` (and `gpt-4o-mini 2024-07-18`) are
     blocked for new deployments; swapped to **`gpt-5-mini 2025-08-07`** chosen from the live
     catalog (500k TPM GlobalStandard quota in eastus2).
  2. **SKU** — GPT-5-family models are **GlobalStandard**/DataZoneStandard, not regional
     `Standard`; wired `chatSku = GlobalStandard` through `main.bicep`.
  3. **Project creation** needs **`allowProjectManagement: true`** on the account (added).
- **RBAC:** azd didn't populate `AZURE_PRINCIPAL_ID`, so the bicep skipped the role grant;
  granted **Foundry User** to the CLI user directly, and documented setting the principal in
  `infra/README.md`.
- ✅ **Live agent test green:** `FoundryAgentGatewayLiveTests` — the real HomeScout agent
  (gpt-5-mini) called the `estimate_mortgage` tool against the provisioned project and returned
  a grounded answer with the not-mortgage-advice caveat. `/api/copilot/ask` is now live with the
  `AZURE_FOUNDRY_*` env set. Infra changes verified-by-provision, not just compiled.

### Iteration 2 Kickoff — Copilot Evidence Contract + Foundry Provisioning (Plan)

- Planned iteration 2 ("light up the copilot"): the backend defines a structured **evidence
  contract** first (`FigureKind` + `EvidenceItem` + a new `CopilotAnswer.Evidence` field;
  gateway maps tool results → provenance-tagged evidence), which is the seam-first dependency
  that unblocks Codex's Evidence panel + conversation-answer rendering. Spec added to
  [[Copilot Agent Gateway]]; both tracks laid out in [[Work Tracks]].
- Flagged **Foundry provisioning** as user-gated (billable `azd provision`; the lead can't
  self-provision) — it lights up `/api/copilot/ask` and enables the live-verified AgentOps
  `CreateAgentVersion`. Until then the copilot path is contract-complete + mock-verified.

### Frontend Iteration 1 — Workspace + Estimator, Reviewed Against The Design

- Codex built the first frontend slice (design tokens, IBM Plex, workspace shell, API-backed
  mortgage estimator). Lead review found it faithful in *design language* but deviating in
  *information architecture* (it led with the estimator; the design leads with the copilot
  conversation and places the estimator as a right-rail panel).
- Per "fix issues, no deferring", reworked the frontend to the design's IA: conversation is
  the main surface (composer → `/api/copilot/ask`, graceful 503); the estimator moved into
  the right-rail Estimator tab with the design's labels (deposit £ + %, "your figure", Total
  repayable, +3% stress payment, Live/Cache provenance); left rail gained the filter + Case
  file/Preferences/Settings; mobile collapses the nav into a hamburger drawer.
- Fixed two defects found in review: the segmented control (fragile legend-in-grid) and a
  flexbox collapse that clipped the metric rows to 2px. Fixed the Playwright `webServer`
  command (a stray `--` made vite ignore `--host/--port`, hanging CI e2e).
- Verified visually (light/dark, desktop/mobile) against the design and **live end-to-end**
  against the running API (contract matches field-for-field; string enum accepted). Component
  + e2e tests updated; `pnpm build/lint/test/e2e` green.

### Advanced CodeQL Setup (Repo-Controlled)

- Replaced the CodeQL default setup with a repo-controlled `.github/workflows/codeql.yml`
  (advanced setup) covering all three shipped languages: `csharp`, `javascript-typescript`,
  and `actions`. C# uses `build-mode: manual` with a pinned .NET 10 build (matching
  `backend-ci`) for a thorough, reliable analysis; least-privilege job permissions;
  `security-extended` queries; PR + push-to-main + weekly triggers. Merged via PR #26 (all
  checks green, including `Analyze (csharp)`).

### Backend Iteration 1 — AgentOps Manifest Step

- Built `HomeScoutCopilot.AgentOps` (new `dotnet/tools/` folder + `/tools/` solution folder):
  `agentops manifest [--out <path>]` assembles the declarative agent manifest from the
  single-sourced agent definition — `AgentPrompt` (versioned prompt) + `HomeScoutAgentTools`
  (refactored to expose `ToolNames` + name constants as the single source used by `Build()`).
- Generated + committed `dotnet/src/HomeScoutCopilot.API.Service/Prompts/homescout.agent.yaml`
  (name / model / instructions_file / tools) by running the tool itself. Added
  `HomeScoutCopilot.AgentOps.Test` (3 tests: definition assembly, YAML shape, and a **drift
  guard** that fails if the committed manifest goes stale vs the code).
- Live `AgentAdministrationClient.CreateAgentVersion` registration is deliberately **deferred**
  to a live-verified slice (needs `azd` provision) — per seam-first / "don't ship an unverified
  integration". Solution builds (0 warn); all fast tests green (AgentOps 3 + existing suite
  unedited). Updated [[GenAIOps Tooling Plan]], [[Component Architecture]], [[Work Tracks]].

### Design Handoff For Codex + Iteration 1 Plan

- The finished Claude Design was committed as `wiki/raw/HomeScout Copilot.html` (compiled,
  self-rendering prototype; editable `.dc.html` source stays in Claude Design — see
  [[Frontend from Claude Design]] memory). Inspected it: IBM Plex Sans/Mono; dark+light token
  themes scoped on the app root (surfaces, brand navy/accent, semantics, **provenance
  live/cache/fallback**, `--dv1..5` data-viz); three-region responsive workspace (header /
  saved-comparisons+nav / main copilot+comparison / evidence panel) at breakpoints <760 / <1180
  / ≥1180; a scripted phase-machine demo. Content maps 1:1 to our API (mortgage estimator,
  copilot, comparison, evidence/provenance).
- Wrote [Codex Frontend Instructions](../__plans/02-frontend/codex-frontend-instructions.md):
  how to work with the design HTML, lift tokens/copy, reproduce the design system + responsive
  layout, wire the API seam (don't mock live endpoints), keep the guardrails
  (not-mortgage-advice, crime=context, fact/estimate/assumption/missing + provenance), and the
  first slice (tokens + shell + mortgage estimator end-to-end, no Foundry needed).
- Set the **iteration 1 plan** in [[Work Tracks]]: Codex builds the frontend first slice;
  lead builds the `AgentOps` manifest step (offline); then lead reviews both, merges
  individually, runs an E2E check. Flagged the structured-evidence contract as the iteration-2
  seam item.

### GenAIOps Tooling Decision + Plan Review (Parallel-Work Ready)

- **Decision:** two dedicated .NET tool projects under a new `dotnet/tools/` — added
  [[GenAIOps Tooling Plan]] (`HomeScoutCopilot.AgentOps` = deploy/manage agents, indexes,
  datasets; `HomeScoutCopilot.Evaluator` = run evaluations). Both reference `.API.Service`
  (single-sourced agent definition), keyless, build with the solution; deploy/eval live runs
  are `[Category("External")]`, off the blocking gate. Recorded the `tools/` scaffolding as
  on-real-need in [[Plan Divergence]] and reconciled with the master plan's
  "no premature scaffolding" rule.
- **Parallel work:** added [[Work Tracks]] — frontend track (owns `frontend/`, calls the API
  via `HomeScoutApiClient`, never touches agent code) vs backend/GenAIOps track (owns
  `dotnet/` + `tools/` + `infra/`), with the **API seam** (Shared DTOs + endpoints + typed
  client) as the only coupling and a seam-first contract-change rule. Lets another agent take
  the frontend while this one does the backend.
- **Plan review / sync fixes:** rewrote the stale [[Readiness Checklist]] (it listed the
  shipped agent gateway + prompt inventory as "not started" and pointed at building the
  long-done workspace shell) to current reality + per-track next steps. Updated
  [[Component Architecture]] with the planned `tools/`; pointed phased-plan Phase 3 at the
  tool projects; firmed the reference doc's tooling location. Drift 0 fail; all new
  wikilinks resolve.

### .NET SDK Spike (GenAIOps) + Frontend Implementation Plan

- **Spike:** reflected over the restored SDK assemblies (`Azure.AI.Projects` 2.0.0,
  `Azure.AI.Projects.Agents` 2.0.0, `OpenAI` 2.10.0, `Azure.AI.Extensions.OpenAI` 2.0.0)
  to answer whether HomeScout can do persisted-versioned agents + evaluation in **pure
  .NET**. Answer: **yes, end-to-end** — `AIProjectClient.AgentAdministrationClient`
  exposes `CreateAgentVersion` / `CreateAgentFromManifest` (+ `ProjectsAgentDefinition` /
  `DeclarativeAgentDefinition`); `AIProjectClient.GetProjectOpenAIClient().GetEvaluationClient()`
  gives `OpenAI.Evals.EvaluationClient` (create/run/output-items); datasets via
  `AIProjectClient.Datasets`; runtime reference-by-name via `AgentReference`.
- **Decision (total .NET stack):** no Python in the repo; the lab's Python scripts are
  guidance only. Recorded in [[Plan Divergence]]; resolved the two open questions in
  [[GenAIOps Reference Implementation]] (with a Python→.NET surface table) and updated
  [[Phased Learning And Build Plan]] Phase 3 to the confirmed .NET path.
- **Frontend:** added [Frontend Implementation Plan](__plans/02-frontend/frontend-implementation-plan.md)
  — the frontend build phase (Stage 1 review the design brief + design-agent deliverables;
  Stage 2 implement design system → screens → copilot surface against the API, with
  component/E2E/a11y tests). Indexed in the 02-frontend + plans READMEs and pointed to from
  the phased plan's Phase 1.
- Drift 0 fail; wikilinks + relative links resolve.

### Infused mslearn-genaiops Patterns Into The Phased Plan + Added As A Reference

- Studied Microsoft's official GenAIOps lab repo (`MicrosoftLearning/mslearn-genaiops`):
  agent deploy script (`agents.create_version` + `PromptAgentDefinition`), declarative
  `agent.yaml`, versioned prompt files, cloud-eval pipeline (Foundry Evals API, built-in
  intent/relevance/groundedness evaluators), batch experiment harness (token/cost capture),
  and the CI eval-as-PR-comment workflow (Azure OIDC login).
- Authored [[GenAIOps Reference Implementation]] — a patterns catalog mapping each to
  HomeScout's .NET translation and owning phase, with the key contrast that we build the
  agent **in-process** (`AsAIAgent`) vs the lab's **persisted, versioned** agent
  (`create_version` + reference-by-name), and an open .NET-SDK spike + eval-tooling-language
  decision.
- Infused concrete "reference pattern" callouts into [[Phased Learning And Build Plan]]
  phases 3 (agent manifest, persisted versioned agent, cloud-eval harness), 4 (batch
  experiments), 6 (CI eval gate w/ OIDC, non-blocking), 7 (fuller azd/bicep infra).
- Added the repo as a reference: linked from [[GenAIOps Learning Path Integration]], indexed
  in the plans README + [[Wiki Index]], and added to [[Release Monitoring]] as a watched repo.
- Drift check 0 fail; all wikilinks + relative links resolve.

### Externalised The Agent Prompt To A Versioned Asset (GenAIOps)

- Moved the Foundry agent's system instructions out of a hardcoded `const` in
  `FoundryAgentGateway` into a versioned, embedded asset
  (`HomeScoutCopilot.API.Service/Prompts/homescout.v1.md`) loaded by a new `AgentPrompt`
  loader (with a `Version` constant). Follows Microsoft's GenAIOps prompt-versioning
  guidance (treat prompts as first-class, git-taggable assets). Added `AgentPromptTests`
  (fast) locking the load path + guardrails; all existing contract/BDD/endpoint tests pass
  unedited (behaviour-lock). Recorded in [[Plan Divergence]] with the Learn links.
- Confirmed the auth + agent-creation model: we authenticate keyless with
  `DefaultAzureCredential`; `AsAIAgent(...)` builds an **in-process** agent (Responses
  path) per gateway construction — it does **not** create/persist a server-side Foundry
  agent resource, so nothing is created "on startup".

## 2026-07-03

### Plan Sync And Readiness Review

- Reviewed the whole repository against the plan set to confirm the plans are in sync and ready to work on.
- Verified link integrity: all `[[wikilinks]]` across the wiki resolve to existing pages.
- Verified the full `dotnet build HomeScoutCopilot.sln` now succeeds (0 warnings, 0 errors); the earlier 2026-07-02 sandbox stall no longer applies.
- Verified `npm install` + `npm run build` in `frontend/` produce a clean Vite production build.
- Confirmed code matches plan claims: API exposes only `/api/status` and `/api/comparison/sample`; React workspace shell present; no Blazor; no Foundry packages yet.
- Fixed a corrupted stray table row fused onto the title in [[Course Playlist Tracker]].
- Updated [[Onboarding Article]] to reflect the current comparison workspace shell instead of a bare starting screen.
- Updated [[Testing Strategy]] to record the successful full-solution and frontend builds.

### Fixed CodeQL Workflow-Permissions Findings

- CodeQL (GitHub Advanced Security default setup) was enabled and flagged 5 medium alerts: each `.github/workflows/*.yml` ran with the broad default `GITHUB_TOKEN` (no `permissions:` block). Added least-privilege `permissions: contents: read` to all five (plan-drift, backend-ci, frontend-ci, external-checks, infra-ci) — they only checkout + build/test, none write to the repo.
- The separate CodeQL "csharp configuration not found" warning is a default-setup comparison quirk (the C# scan on `main` hadn't completed when the PR was evaluated), not a code defect; it resolves once a C# scan completes on `main`. If C# default-setup analysis fails on net10, switching to advanced setup (a `codeql.yml` with a custom `dotnet build`) is the follow-up.

### Implemented API Vertical Slices + Validated Options + Shared Rename

- **Renamed** `HomeScoutCopilot.Shared.Application` → `HomeScoutCopilot.Shared` (project, csproj, namespace `HomeScoutCopilot.Shared.Contracts`, all refs, `.slnx`); test project → `HomeScoutCopilot.Shared.Test`.
- **Vertical slices (RagLab parity):** Carter 10 + MediatR 12.5.0 (pinned free/Apache-2.0; v13+ is commercial) + FluentValidation 12. Moved the four endpoint groups into `.API/Features/{Status,Comparison,Mortgage,Copilot}/` as Carter `ICarterModule`s delegating to MediatR commands/queries/handlers (calling the `.API.Service` services). `Program.cs` is now thin (`AddMediatR` + `AddCarter` + `AddValidatedOptions`; `app.MapCarter()`).
- **Validated options:** `IValidatedOptions<T>` + `AddValidatedOptions<T>()` helper in `.API.Service/Settings/`; `BaseRateOptions`/`FoundryOptions` moved there, self-declare their section + a FluentValidation validator, validated on startup.
- **Behaviour-locked:** all 33 fast tests (contract, mortgage BDD, copilot 200/503/400) passed **unedited**, and the Aspire boot test is green — routes/shapes/behaviour unchanged; only structure changed. Recorded divergences (kept project roles, helper in `.API.Service`, deferred request-validation pipeline) in [[Plan Divergence]]; updated [[Component Architecture]] and marked the plan implemented.

### Studied RagLab's API + Planned Vertical-Slice Parity

- Studied RagLab's API structure: `Features/<X>/` **vertical slices** as Carter `ICarterModule`s with thin endpoints delegating to **MediatR** commands/queries/handlers (co-located DTOs/validation/mapping), rich endpoint metadata, and a `Settings/` **validated-options** convention (`IValidatedOptions<T>` self-declares its section + a FluentValidation validator, validated on startup). Host is `.API.Service`; feature library is `.API` (reverse of HomeScout's naming).
- Decision: **full parity** (Carter + MediatR + FluentValidation + validated options), **plan first**. Wrote [API Vertical Slices + Validated Options — Plan](../wiki/__plans/03-backend/api-vertical-slice-plan.md): target structure, endpoint/handler/validation/options patterns, feature inventory (Status/Comparison/Mortgage/Copilot), packages (flagged **MediatR v13+ commercial licensing** → pin v12 free), behaviour-locked migration steps (existing contract/BDD/endpoint tests must pass unedited), acceptance criteria. Keep HomeScout's project roles (`.API` host, `.API.Service` app layer) — a recorded divergence from RagLab's inverted split.

### Authored The Full Design Brief (Design-Agent Ready)

- Wrote `wiki/__plans/02-frontend/design-brief.md` — a complete, design-agent-ready specification for a premium, world-class, full-scope design: product vision & positioning, brand voice, personas/JTBD, design principles, full information architecture, every screen/region + the copilot conversation UX in depth, data/trust primitives (source badge, provenance, assumption callout, fact/estimate/assumption/missing), key flows, data-viz, a design-system token spec (colour incl. light/dark, type, spacing, motion), component inventory, all states, responsive/breakpoints, WCAG 2.2 AA accessibility, microcopy, constraints, anti-patterns, and explicit deliverables.
- Indexed it in the plans READMEs and linked it from [[Frontend Design Guidelines]] (which remains the binding rules).

### Wired The Copilot Endpoint (Slice 4)

- Copilot Slice 4: `POST /api/copilot/ask` → `IHomeScoutAgentGateway` → grounded `CopilotAnswer`. DI registers `FoundryAgentGateway` + `HomeScoutAgentTools` + `TokenCredential` (`DefaultAzureCredential`) **only when a Foundry endpoint is configured** (`Foundry:ProjectEndpoint` or the azd output `AZURE_FOUNDRY_PROJECT_ENDPOINT`); scoped to avoid a captive HttpClient.
- Endpoint returns **503** when the copilot isn't configured and **400** on an empty message. Typed `HomeScoutApiClient.AskCopilotAsync`. Added `Azure.Identity` to `.API`.
- Offline tests (no Azure): endpoint with a fake gateway via `ConfigureTestServices` → 200 + body; unconfigured → 503; empty message → 400. API.Test now 28 fast tests; quality gate green.
- Seam-first status: fully offline-testable; the **real endpoint lights up once Foundry is provisioned** (`azd provision` sets `AZURE_FOUNDRY_*`) — live agent path still pending verification. React conversation surface is the next slice.

### Built The Foundry Agent Gateway (Slice 3)

- Copilot Slice 3: `FoundryAgentGateway` (`.API.Service`) — the real adapter using the new **Microsoft Agent Framework** (`Microsoft.Agents.AI.Foundry` 1.5.0, Responses path): `new AIProjectClient(endpoint, credential).AsAIAgent(model, name, instructions, tools)`, handed the Slice-2 `AIFunction` tools; `AskAsync` runs the agent (framework runs the tool loop) and returns a grounded `CopilotAnswer` with the tool calls made. `FoundryOptions` binds the azd outputs; `TokenCredential` injected.
- Verified the exact API on Microsoft Learn before coding; used `dotnet build` as the offline verification loop (one fix: `AsAIAgent(tools:)` wants `IList<AITool>`). Full solution compiles; fast gate green (API.Test 25 fast tests).
- `FoundryAgentGatewayLiveTests` `[Category("External")]`+`[Category("Integration")]` — reads `AZURE_FOUNDRY_PROJECT_ENDPOINT`/`AZURE_FOUNDRY_MODEL_DEPLOYMENT`, asserts the agent calls `estimate_mortgage` + the not-advice caveat. **Skips cleanly** when unset (confirmed), so it runs live only where Foundry is provisioned + creds exist. Added `Azure.Identity` to the test project.
- Seam-first honest status: gateway **compiles**, **not yet live-verified** (needs `azd provision` + Azure creds; CI OIDC wiring deferred). DI registration + `POST /api/copilot/ask` endpoint are Slice 4.

### Confirmed Basic Agent Setup After Cosmos Docs Check

- Checked whether Foundry agents need Cosmos (they do for BYO/Standard). Verified via Microsoft docs (capability hosts + standard agent setup) that there is **no Cosmos-only** path: the project capability host requires Cosmos **+** AI Search **+** Storage together (only your-own-Azure-OpenAI optional), or none (all Microsoft-managed).
- Decision: keep **Basic** setup for the cost-answer slice (Microsoft-managed threads; the bicep I authored is unchanged). Upgrade to full **Standard** (Cosmos ≥3000 RU/s + Storage + AI Search + Key Vault + 2 capability hosts + RBAC) as a dedicated step before data residency / server-side threads / RAG matter.
- Recorded the finding + upgrade path in the design plan and as a [[Plan Divergence]] entry (Basic vs RagLab's Standard, with rationale). No code change.

### Authored Foundry Provisioning (Slice 1, azd + bicep)

- Copilot Slice 1: reproducible provisioning. `azure.yaml` + `infra/` bicep (`main.bicep` subscription-scope → `modules/foundry-account.bicep` + `modules/foundry-project.bicep`): Foundry account (`Microsoft.CognitiveServices/accounts`, AIServices, S0, custom subdomain, system-assigned identity) → chat model deployment (`gpt-4.1-mini`, Standard quota) → Foundry project (separate module after the account settles) → RBAC (deployer gets **Foundry User** `53ca6127-…` for local-dev agent data-plane access). Grounded in RagLab's bicep; deferred Cosmos/AI Search/Document Intelligence.
- Outputs wired for azd → `.env`: `AZURE_FOUNDRY_PROJECT_ENDPOINT`, `AZURE_FOUNDRY_MODEL_DEPLOYMENT` (a later slice reads them as `FoundryOptions`).
- Seam-first verification: bicep **compiles** (`az bicep build`, exit 0) and is enforced by a new `infra-ci.yml`; **not yet `azd up`-verified** — provisioning is proven by running `azd provision` with Azure creds (user/CI; the sandbox can't). Added `infra/README.md` with the `azd` steps. `infra/` is now justified (real Azure need), consistent with the earlier "no premature scaffolding" deferral.

### Made "Seam-First" A Binding Instruction

- Codified the Slice-2 approach as an engineering standard in `AGENTS.md`: for external/hard-to-provision dependencies, prove the shape + real logic offline behind an interface, use a test-double fake (never shipped) for wiring, then implement the real adapter last and verify it live (`[Category("External")]`). The fake proves the shape, not the dependency — it never substitutes for verifying the real thing (pairs with "verify, don't assume").

### Built The Agent Gateway + Tools (Slice 2, offline)

- Copilot Slice 2 (no Azure): `IHomeScoutAgentGateway` boundary + `CopilotRequest`/`CopilotAnswer`/`CopilotToolCall` DTOs (`.Shared.Application`).
- `HomeScoutAgentTools` (`.API.Service`) exposes the estimator + base rate as real `Microsoft.Extensions.AI` `AIFunction`s via `AIFunctionFactory.Create` (`estimate_mortgage`, `get_base_rate`) — the same tools the Foundry agent (Microsoft Agent Framework) will be handed in Slice 3.
- `FakeHomeScoutAgentGateway` test double for offline contract/endpoint tests. Tests invoke the `AIFunction`s directly and assert routing (estimate → £1,500.75, base rate → 3.75%) with no LLM/Azure; API.Test now 25 fast tests, gate green.
- Added `Microsoft.Extensions.AI` 10.7.0 to `.API.Service`. Updated the design (Slice 2 done), component architecture, feature coverage.

### Corrected The Agent SDK To The Microsoft Agent Framework

- Correction: the copilot uses the **new Microsoft Agent Framework** (1.0 GA), not the classic `PersistentAgentsClient` (`Azure.AI.Agents.Persistent`, being phased out). Verified on Microsoft Learn.
- SDK: `Microsoft.Agents.AI` + `Microsoft.Agents.AI.Foundry` + `Azure.AI.Projects` + `Azure.Identity`; `AIProjectClient.AsAIAgent()` / `CreateAIAgent(...)`; tools are C# methods wrapped with `AIFunctionFactory.Create(...)` and the framework runs the tool-call loop (no manual submit-outputs).
- Studied RagLab's bicep for the resource set and recorded the concrete provisioning shape for the cost-answer slice: Foundry account (`Microsoft.CognitiveServices/accounts`, kind AIServices) → chat model deployment (stable role-label name; SKU/quota caveats) → Foundry project (`.../accounts/projects`, deployed as a separate module after the account settles) → RBAC/managed identity. Deferred Cosmos (thread storage) + AI Search + Document Intelligence to their phases (RAG/persistence).
- Updated [[Copilot Agent Gateway]] and [[API-First Foundry Agents]].

### Designed The Copilot Agent Gateway (Foundry Agent Service)

- Reframed the roadmap around the product being a **copilot**, not a form-driven frontend: the deterministic tools (mortgage estimator, base rate) are capabilities the agent *calls*; the frontend is a conversation surface.
- Decisions: real Foundry Agent Service (`PersistentAgentsClient` + `DefaultAzureCredential`, function tool-calling); reproducible provisioning via **azd + bicep** (justifies `dotnet/infra/` now); companion repo inspiration-only (different framework); first slice = conversational cost answer.
- Verified the current approach on Microsoft Learn (Foundry function-calling flow: create agent → thread → run → on requires-action execute our tool → submit outputs; azd `azd up` provisioning). `az`/`azd` confirmed installed locally.
- Wrote [Copilot Agent Gateway — Design](../wiki/__plans/03-backend/copilot-agent-gateway-plan.md): request flow, tool definitions over the built services, agent instructions (prompt governance), azd/bicep provisioning + RBAC/managed identity, `POST /api/copilot/ask` + DTOs, and the verify-don't-assume strategy (offline fake gateway in the gate; live Foundry `[Category("External")]` test where creds exist — the sandbox can't provision/verify Azure). Sequenced into 4 implementation slices.

### Implemented The Mortgage Cost Estimator (MVP)

- Built HomeScout's first real capability per the design: `IMortgageCostEstimator` (`.API.Service`) — pure, deterministic amortisation (repayment + interest-only, `i=0` edge, +3% stress, LTV), computed in decimal (no floating point). `MortgageEstimateRequest`/`MortgageEstimateResult` DTOs in `.Shared.Application`.
- FluentResults validation → 400 ProblemDetails (price/deposit/rate/term/type rules); `POST /api/mortgage/estimate` (thin endpoint); `HomeScoutApiClient.EstimateMortgageAsync`; string-enum JSON for `RepaymentType`.
- Tests: unit vectors (£270k/4.5%/25y ≈ £1,500.75, interest-only £1,012.50, zero-rate £1,000.00, monotonicity, 6 validation cases) + `MortgageEstimate.feature` BDD through the wired endpoint + client. API.Test now 20 fast tests; quality gate green.
- Safety: generic illustrative calculator on the buyer's own rate, assumptions + not-mortgage-advice caveat in every result. Updated design page (status: implemented), endpoint summary, component architecture, feature coverage, readiness.

### Made "Verify, Don't Assume" A Binding Instruction

- Added the external-dependency verification principle to `AGENTS.md` > Engineering Standards: prove integrations end-to-end with a live test, keep it out of the blocking gate (`[Category("External")]` + scheduled run), degrade gracefully, and make prod report the served path. Bar: "we know it works, and we'll know the moment it stops" — not "we hope it works".

### Verified The Live Base-Rate Fetch (Not Just The Fallback)

- Point taken: don't ship a live integration we've never seen succeed. The earlier 403 was WebFetch's restricted fetcher, not reality — a real HTTP client with a browser User-Agent gets `HTTP 200` + CSV from the BoE endpoint (confirmed via `curl`, including the exact URL format the code builds).
- Aligned the code to the verified request (`CSVF=TN`) and added `BaseRateLiveTests` — a live test that fetches through the fully-wired app and asserts `Provenance == "Live"` (a `Fallback` result is the failure signal). **It passes** — the live path genuinely works end-to-end.
- Kept it out of the PR gate (`[Category("External")]` + `[Category("Integration")]`) so a third-party outage can't block merges, and added `.github/workflows/external-checks.yml` (nightly + on demand) to run `--filter Category=External` and alert if BoE blocks us or changes format.
- General principle recorded in [[Quality Gate & Test Plan]]: external dependencies are verified (live test) not assumed, kept out of the blocking gate, degrade gracefully (fallback), and are observable in prod (Live/Cache/Fallback provenance).

### Implemented The Bank Of England Base-Rate Provider

- Built the live base-rate provider (production-grade) ahead of the estimator: `IBaseRateProvider` in `.API.Service` with `BankOfEnglandBaseRateProvider` — fetches the official Bank Rate series (`IUDBEDR`) from the BoE Interactive Database CSV, caches ~1 day (`IMemoryCache`), and falls back to a configured last-known value; **never throws** (base rate is context-only).
- Resilience via ServiceDefaults (`AddStandardResilienceHandler` on all HttpClients) + a descriptive User-Agent (the BoE endpoint 403s requests without one). Source URL/series/fallback are configurable (`BaseRateOptions`, `appsettings` `BaseRate` section).
- Endpoint `GET /api/mortgage/base-rate` (always 200, provenance `Live`/`Cache`/`Fallback`); typed `HomeScoutApiClient.GetBaseRateAsync`; `BaseRate` DTO in `.Shared.Application` (documented as orientation only, not a product rate).
- Tests (no live network): parser unit tests, live→cache, fallback-on-throw, fallback-on-non-success (via a stub `HttpMessageHandler`), and an endpoint contract test with the provider stubbed (`ConfigureTestServices`). `InternalsVisibleTo` for the parser. API.Test now 9 fast tests; quality gate green.
- Verified the authoritative source (GOV.UK/BoE) before coding; live connectivity to be validated in a real environment. Updated design page, endpoint summary, component architecture, feature coverage.

### Designed The Mortgage Cost Estimator (MVP)

- Researched the authoritative UK basis (GOV.UK MaPS Algorithmic Transparency Record, MoneyHelper, FCA MCOB 4, Bank of England) and wrote [Mortgage Cost Estimator — Design (MVP)](../wiki/__plans/03-backend/cost-estimator-mortgage-plan.md) under `03-backend`.
- Scope: mortgage repayment only. Standard amortisation (monthly rate = annual/12, 2dp rounding, +3% stress test — mirroring MaPS); repayment and interest-only; request/result DTOs; FluentResults validation → ProblemDetails; placement behind `IHomeScoutService` with a `POST /api/mortgage/estimate` endpoint and a typed client method; unit + Reqnroll BDD test plan.
- Safety: framed as a generic illustrative calculator (information, not FCA-regulated advice — no named product, user-supplied rate, assumptions + "not mortgage advice" caveat). BoE base rate (3.75%, June 2026) noted as rate *context only*, not a computed default.
- Indexed the plan in `wiki/__plans/README.md` and `03-backend/README.md`; updated readiness checklist, phased plan, and endpoint summary; full monthly cost-of-ownership deferred.

### Added Engineering Standards To Docs

- Recorded that HomeScout is built as production-grade software to Microsoft's standards.
- Added a binding "Engineering Standards" section to `AGENTS.md` and a cross-referenced version in [[Coding Conventions]]: follow .NET Framework Design Guidelines / C# conventions, ASP.NET Core + Aspire + Microsoft Foundry Agent Service guidance, the Azure Well-Architected Framework, and Entra/managed-identity least privilege; use current non-deprecated APIs (verify on Microsoft Learn); treat security, accessibility, and observability as first-class.

### Frontend Switched To pnpm

- Resolved the last open migration decision: frontend now uses **pnpm** (RagLab parity), replacing npm.
- Added `packageManager: pnpm@11.1.3` to `frontend/package.json`; moved pnpm settings to `frontend/pnpm-workspace.yaml` (`allowBuilds: esbuild: true`, `overrides.postcss`) since pnpm 10+ no longer reads the package.json `pnpm` field; generated `pnpm-lock.yaml`, removed `package-lock.json`.
- AppHost `AddViteApp(...).WithPnpm()`; `playwright.config` webServer uses pnpm; `scripts/quality-gate.sh` and `frontend-ci.yml` use pnpm (`pnpm/action-setup@v4`, `pnpm install --frozen-lockfile`, `pnpm exec playwright install`).
- Verified: pnpm build/lint/test/e2e all pass; Aspire integration test launches Vite via pnpm; full quality gate PASS. Updated plan (open decisions now none), AGENTS, README.

### RagLab Skeleton Migration — Phase 4 (E2E) + migration complete

- Added Playwright (chromium) e2e to `frontend/`: `playwright.config.ts` (build + `vite preview` webServer on :4173), `e2e/workspace.spec.ts` smoke (core regions visible). Added `e2e` npm script.
- Broadened Vitest component tests (composer textbox + Generate/Attach buttons + evidence items).
- Made the Vite `/api` proxy conditional on the Aspire target env so standalone `vite preview` (e2e) no longer logs proxy errors.
- Wired e2e into `frontend-ci.yml` (required step: `playwright install --with-deps chromium` → `npm run e2e`) and into `scripts/quality-gate.sh` (guarded, auto-installs chromium). Gitignored Playwright artifacts.
- Full quality gate PASS: drift 0/0, backend 8 fast tests, frontend 2 unit + 1 e2e.
- **Migration complete** — HomeScout now mirrors the RagLab skeleton (dotnet/ + .slnx, layered API with FluentResults, NUnit + Reqnroll BDD + Allure, plan-drift + CI quality gate, per-phase branch→PR→merge), with intentional divergences recorded (frontend at repo root, FluentResults over custom Result, no premature poc/infra/byo scaffolding).
- Delivered on branch `migration/phase-4-e2e`.

### RagLab Skeleton Migration — Phase 3 (Backend Layering + Reqnroll BDD)

- Split the API into layered projects (RagLab parity): `.API` (thin minimal-API host), `.API.Service` (`IHomeScoutService`, returns FluentResults), `.API.Client` (typed `HomeScoutApiClient`), `.Shared.Application` (DTOs), `.Functional` (FluentResults→ProblemDetails mappers). Renamed `HomeScoutCopilot.ApiService` → `.API` (Aspire resource kept as `apiservice` so the Vite proxy env is stable).
- Endpoints now resolve `IHomeScoutService` and map `Result<T>` via `.ToHttpResult()`; the Phase 1 contract tests passed **unchanged** (behaviour-lock held).
- Split tests: `.API.Test` (contract + Aspire integration + BDD), `.Shared.Application.Test` (contract serialization), `.Functional.Test` (mapper: Ok→200, Fail→400 ProblemDetails, Ok→204). All NUnit.
- Added Reqnroll BDD to `.API.Test`: `Reqnroll.NUnit` + `Allure.Reqnroll`, `Features/Status.feature`, `StepDefinitions/`, `Drivers/ApiDriver` (WebApplicationFactory + typed client). Scenario passes; Allure writes results under `bin/`. Deferred `Bogus`/`Testcontainers` until a scenario needs them.
- Chose FluentResults 3.16.0; drift check FluentResults convergence warn cleared to ok (0 fail, 0 warn). Gitignored generated `*.feature.cs` and `allure-results/`.
- Updated [[Component Architecture]], [[Feature Coverage]], README, quality-gate-plan.
- Delivered on branch `migration/phase-3-layering`.

### RagLab Skeleton Migration — Phase 2 (Directory Relocation)

- Moved all .NET projects under `dotnet/` (`git mv` → `dotnet/src/{AppHost,ServiceDefaults,ApiService}`, `dotnet/tests/HomeScoutCopilot.Tests`); history preserved.
- Converted `HomeScoutCopilot.sln` → `dotnet/HomeScoutCopilot.slnx` with `/src` and `/tests` solution folders.
- Fixed the only broken references: Tests project refs (`..\..\src\...`), AppHost's Vite path (`../frontend` → `../../../frontend`), and `aspire.config.json` appHost path.
- Updated `backend-ci.yml` to the `.slnx`; `quality-gate.sh` auto-detects it; `check-plan-drift.sh` scans repo-wide so needed no path change.
- Deliberately did **not** mirror RagLab's `dotnet/poc/`, `dotnet/infra/`, or `scripts/byo/` as empty scaffolding; recorded in the plan that infra/byo arrive with Azure deployment (Phase 7) and poc only if we spike experiments. Companion repo stays an external reference.
- Updated root `README.md` and [[Component Architecture]] to the `dotnet/` layout.
- Verified: `dotnet build`/`dotnet test dotnet/HomeScoutCopilot.slnx` green (3/3, incl. the Aspire integration test which launched Vite from the new path); `scripts/quality-gate.sh` PASS.
- Delivered on branch `migration/phase-2-relocation`.

### RagLab Skeleton Migration — Phase 1 (Quality Gate + Governance)

- Stood up the quality gate before any code moves so Phases 2–4 run inside a green gate.
- Added `scripts/check-plan-drift.sh` (POSIX; enforces plan-index/link integrity, no stale `wiki/plan/` refs, API-first frontend, FluentResults-not-ErrorOr) and `scripts/quality-gate.sh` (runs drift + backend + frontend). Drift reports 0 fail, 1 forward-looking warn.
- Added CI workflows `plan-drift`, `backend-ci` (fast non-integration tests), `frontend-ci` (build + lint + unit) and `.github/copilot-instructions.md` → AGENTS.md.
- Rewrote `AGENTS.md` RagLab-style: Plan Sync Protocol, non-negotiable invariants, git workflow, preserved attribution rule and product/frontend/course rules.
- Seed tests: switched `HomeScoutCopilot.Tests` to **NUnit** (RagLab parity, ahead of Reqnroll BDD in Phase 3); backend `ApiContractTests` (NUnit, in-memory `WebApplicationFactory`, asserts `/api/status` + `/api/comparison/sample` shape; behaviour-lock for later phases); frontend Vitest + Testing Library smoke test for the workspace shell. Categorized the Aspire `WebTests` as `[Category("Integration")]` and added `ApiMarker` so the API can boot without a `Program` type clash.
- Recorded the test-framework decision (NUnit + Reqnroll + Allure) and a Phase 3 Reqnroll BDD workstream in the master migration plan; answered that HomeScout did not yet have raglab's Reqnroll BDD tests.
- Added `wiki/__plans/04-testing/quality-gate-plan.md`; renamed `NuGet.Config` → `nuget.config`.
- Verified locally: `scripts/quality-gate.sh` → PASS.
- Delivered on branch `migration/phase-1-quality-gate`.

### RagLab Skeleton Migration — Phase 0 (Plan Foundation)

- Decided to restructure HomeScout to mirror the RagLab (`HBK.Insights.Raglab`) skeleton; authored the master sequenced migration plan as the source of truth (`wiki/__plans/00-roadmap/homescout-skeleton-migration-plan.md`).
- Locked decisions: full clone incl. backend layering, frontend stays at repo root, FluentResults for the `.Functional` layer.
- Stood up `wiki/__plans/` with numbered phase folders (`00-roadmap … 04-testing`) and a `00-roadmap/course/` group for the playlist tracker and video notes.
- Migrated all existing `wiki/plan/` pages into the new structure with `git mv` (history preserved); added placeholder READMEs for the empty phase folders.
- Rewrote `wiki/__plans/README.md` as the full index plus a "Detecting Plan Drift" section.
- Rewired every explicit `wiki/plan/` path reference in `AGENTS.md`, root `README.md`, `wiki/index.md`, and moved plan files to the new locations; historical `wiki/log.md` entries left intact.
- Verified: all 25 README relative links resolve; no active reference to the old `wiki/plan/` path remains.
- Delivered on branch `migration/phase-0-plan-foundation` via PR (per-phase branch → PR → merge workflow).

## 2026-07-02

### Repository Created

- Created HomeScout Copilot as a .NET Aspire + Blazor solution.
- Added `AppHost`, `ServiceDefaults`, `Web`, `ApiService`, and `Tests` projects.
- Added an initial HomeScout-branded Blazor workspace page.
- Added `NuGet.Config`.
- Pinned `Microsoft.OpenApi` to avoid the vulnerable transitive `2.0.0` package.
- Verified `dotnet restore`.
- Verified NuGet vulnerability audit returned no vulnerable packages.
- Full solution build was attempted but the build process became stuck in the sandbox with no output and could not be terminated due restricted process controls.

### Repository Moved

- Moved the repository to `/Users/olaheavy/source/code/rag/home-scout-pilot`.
- Verified Git history and clean working tree after the move.

### Wiki Structure Created

- Added canonical wiki structure under `wiki/`.
- Migrated existing product brief, playlist tracker, and video notes into `wiki/plan/`.
- Added `AGENTS.md` and planned `CLAUDE.md` symlink so assistant rules are shared.
- Added plan divergence workflow so future plan/tool comparisons use `wiki/plan/` as the canonical source.

### Removed Legacy Docs Compatibility

- Removed the `docs/` compatibility symlink layer.
- Kept `wiki/` as the only documentation home.
- Updated README, agent rules, plan overview, and plan divergence notes so plan checks read `wiki/plan/` directly.

### Planned Available Course Videos

- Refreshed the YouTube playlist feed and confirmed 12 available entries: intro plus Parts 1-11.
- Updated the companion repo clone and confirmed latest state `062d953 Part 11 Done`.
- Added [[Video Implementation Roadmap]].
- Expanded [[Course Playlist Tracker]] with companion-code references.
- Added per-video notes for Parts 2-11 under `wiki/plan/video-notes/`.


### Added Release Monitoring Routine

- Added [[Release Monitoring]] with commands for checking playlist RSS updates and companion repo commits.
- Linked the routine from the wiki index, plan overview, and course playlist tracker.

### Adopted API-First Foundry Agent Direction

- Fact-checked Microsoft Foundry Agent Service direction against current Microsoft Learn docs.
- Added [[API-First Foundry Agents]].
- Updated component architecture, endpoint summary, roadmap, tracker, and plan divergence to make HomeScout API-first and Foundry-agent oriented.

### Pivoted HomeScout To React From Part 1

- Removed the Blazor frontend project from the implementation direction.
- Added a React/Vite frontend as the product frontend path from the first course implementation part.
- Updated plan divergence and video notes so course Blazor code is treated as reference material only.

### Replaced Blazor Frontend With React

- Removed `HomeScoutCopilot.Web` from the implementation.
- Added `frontend/` as a Vite React project.
- Updated `AppHost` to host the React frontend with `AddViteApp`.
- Added `/api/status` and `/api/comparison/sample` endpoints to `HomeScoutCopilot.ApiService`.
- Verified restore, npm install/audit/build, API/AppHost builds, and Aspire integration test.

### Added GenAIOps Learning Path Integration

- Added [[GenAIOps Learning Path Integration]] based on Microsoft's Operationalize generative AI applications learning path.
- Mapped each learning-path module to HomeScout artifacts: architecture records, prompt governance, evaluations, CI automation, monitoring, and distributed tracing.
- Defined how future video notes should include a GenAIOps hook when the feature touches prompts, agents, tools, retrieval, evaluation, monitoring, or tracing.

### Added Phased Learning And Build Plan

- Added [[Phased Learning And Build Plan]] to sequence videos, Microsoft Learn GenAIOps modules, product design, API-first implementation, testing, evaluations, and Azure deployment management.
- Moved product design into the early phases before deep AI behavior.
- Added explicit expectations for AI evaluations in [[Testing Strategy]].

### Verified GenAIOps Modules Individually

- Opened each of the six Microsoft Learn GenAIOps modules and recorded the checked unit lists in [[GenAIOps Learning Path Integration]].
- Adjusted [[Phased Learning And Build Plan]] so full tracing work follows the monitoring/deployment foundation, while lightweight correlation ids can still be introduced earlier.

### Added RAG Architecture To Plan

- Added [[RAG Architecture]] to define HomeScout's two retrieval layers: user-owned case files and the curated HomeScout knowledge base.
- Updated [[Phased Learning And Build Plan]] so curated knowledge starts early as source-controlled assumptions and safety notes, while case-file retrieval arrives with saved comparisons, uploads, and user scoping.
- Updated architecture, feature coverage, and testing notes for case-file retrieval and curated knowledge-base retrieval.

### Added Curated Knowledge Source Strategy

- Updated [[RAG Architecture]] with source rules for the curated HomeScout knowledge base.
- Clarified that HomeScout should store short authored notes with source metadata, not raw scraped external websites.
- Listed seed source families including GOV.UK, HM Land Registry, RICS, Police.uk, OpenStreetMap/Overpass, official school datasets, and internal HomeScout safety rules.

### Reviewed Plans For Start Readiness

- Reviewed the phased plan, video roadmap, GenAIOps plan, RAG architecture, product brief, API-first architecture, endpoint summary, feature coverage, testing strategy, and plan divergence notes.
- Updated stale starter references so docs match the current React/API state.
- Added [[Readiness Checklist]] as the starting gate for implementation sessions.

### Clarified Foundry SDK Direction

- Updated [[API-First Foundry Agents]], [[Overview]], [[Readiness Checklist]], and [[Plan Divergence]] to explicitly say HomeScout will use the new Microsoft Foundry Agent Service SDK/API surface for real agent work.
- Clarified that the course companion repo is a guide for concepts, sequencing, and standard implementation patterns, not the target architecture or SDK surface.
- Updated [[Endpoint Summary]] to describe future tools as Foundry Agent Service tools or backend wrappers rather than course-specific Agent Framework tools.

### Softened Foundry Package Assumption

- Updated [[API-First Foundry Agents]], [[Overview]], [[Readiness Checklist]], and [[Plan Divergence]] so `Azure.AI.Agents.Persistent` is treated as a candidate package to re-check, not a locked dependency.
- Clarified that the first Foundry implementation should prefer the new Foundry project endpoint and Responses API path unless current implementation-time docs indicate a better SDK route.

### Aligned Foundry SDK Plan To Microsoft SDK Overview

- Updated [[API-First Foundry Agents]], [[Overview]], [[Readiness Checklist]], and [[Plan Divergence]] to use Microsoft's Foundry SDK overview as the anchor.
- Clarified that HomeScout should use the OpenAI SDK against Foundry `/openai/v1` for direct model calls, and the Foundry SDK for project, agent, index, evaluation, and tracing platform work.
- Removed `Azure.AI.Agents.Persistent` from the planned default path.
