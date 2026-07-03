# Phased Learning And Build Plan

This is the step-by-step operating plan for HomeScout Copilot. It combines:

- the YouTube playlist and companion repo
- the Microsoft Learn GenAIOps path
- HomeScout product design
- API-first implementation
- testing, evaluations, deployment, and operations

The rule: do not only watch, and do not only build. Each phase has a watch block, a learn block, a build block, and an evidence block.

This plan was checked against all six Microsoft Learn modules on 2026-07-02. See [[GenAIOps Learning Path Integration]] for the module-by-module audit, and [[GenAIOps Reference Implementation]] for the **concrete, adoptable patterns** (versioned agents, prompt manifest, cloud evals, batch experiments, CI eval gate) distilled from Microsoft's official lab repo `MicrosoftLearning/mslearn-genaiops`. Phases 3–7 below cite specific reference patterns to implement.

## Phase 0: Orientation And Product Contract

Purpose:

- Understand the course shape.
- Lock the HomeScout product promise.
- Avoid drifting into a generic chatbot.

Watch:

- Playlist intro.

Microsoft Learn:

- Skim the GenAIOps path overview.
- Start "Plan and prepare a GenAIOps solution."

Build:

- Confirm HomeScout is a property and area comparison assistant, not mortgage advice.
- Keep one branch and one product codebase.
- Keep React from the first frontend phase.
- Keep API-first boundaries.

Evidence:

- [[Product Brief]] is current.
- [[API-First Foundry Agents]] is current.
- [[Plan Divergence]] notes that course Blazor is reference material only.

## Phase 1: Product Design Before Feature Depth

Purpose:

- Make design a first-class engineering phase.
- Define the first usable workspace before building deep AI behavior.

Watch:

- Part 1: repo and Aspire solution.
- Part 2: Blazor baseline, but treat it as behavior reference only.

Microsoft Learn:

- Continue "Plan and prepare a GenAIOps solution."

Build:

- React comparison workspace shell.
- Saved-comparison sidebar placeholder.
- Property input area.
- Comparison/report area.
- Evidence/assumptions panel placeholder.
- API health/status call from React.

Design requirements:

- Follow [[Frontend Design Guidelines]] strictly.
- First screen is the working product, not a landing page.
- Use realistic HomeScout language from the start.
- Prefer compact workflow UI over tutorial chat UI.

Evidence:

- Desktop and mobile screenshots or notes.
- Frontend build passes.
- API build passes.
- [[Component Architecture]], [[Feature Coverage]], and [[Log]] updated.

## Phase 2: API Contract And Deterministic Tooling

Purpose:

- Build the first useful HomeScout capability before adding agent complexity.
- Make the API contract clear.

Watch:

- Part 3: tool calls.

Microsoft Learn:

- Finish "Plan and prepare a GenAIOps solution."
- Start "Manage prompts for agents in Microsoft Foundry with GitHub."

Build:

- `POST /api/comparisons/draft` or equivalent comparison endpoint.
- Deterministic `estimate_monthly_costs` tool — mortgage-only MVP designed in [Mortgage Cost Estimator](./03-backend/cost-estimator-mortgage-plan.md).
- DTOs for property input, assumptions, and comparison result.
- First backend service boundary for future Foundry agent orchestration.
- Start the curated HomeScout knowledge base as source-controlled assumptions and safety notes, not as vector search yet.

Testing:

- Unit tests for cost-estimate calculations.
- API tests for request validation and response shape.
- No live external APIs in tests.

Evidence:

- Tests pass.
- Endpoint documented in [[Endpoint Summary]].
- Tool behavior documented in [[Feature Coverage]].

## Phase 3: Prompt Governance And First Agent Workflow

Purpose:

- Introduce AI only after the product, API, and deterministic tool shape exist.
- Make prompt changes reviewable.

Watch:

- Part 4: reasoning.

Microsoft Learn:

- Complete "Manage prompts for agents in Microsoft Foundry with GitHub."
- Start "Evaluate and optimize AI agents through structured experiments."

Build:

- First prompt inventory folder. ✅ done — `Prompts/homescout.v1.md` embedded + loaded by `AgentPrompt` (reference pattern 3).
- First prompt/agent design record.
- Backend agent gateway interface targeting Microsoft Foundry Agent Service. ✅ done — `IHomeScoutAgentGateway` / `FoundryAgentGateway`.
- Comparison summary generation behind the API.
- Evidence panel that separates facts, estimates, assumptions, and missing data.
- Link prompts and evaluations to curated HomeScout knowledge-base entries for terminology, source reliability, and safety rules.

Reference patterns to adopt ([[GenAIOps Reference Implementation]]):

- **Declarative agent manifest** (pattern 2): a `homescout.agent.yaml` (`name` / `model` /
  `instructions_file`) as the single source of truth alongside the prompt asset.
- **Persisted, versioned agent** (pattern 1): spike whether .NET exposes a
  `create_version` + `PromptAgentDefinition` equivalent; if so, add a deploy step that
  registers the prompt as a versioned Foundry agent and have the gateway reference it by
  name (portal-visible versions + rollback) instead of building it in-process per request.

Testing and evaluation:

- Hand-curated evaluation dataset for 5-10 property comparison scenarios.
- Rubric for usefulness, groundedness, safety, format adherence, latency, and cost.
- Safety checks for "not mortgage advice" and "no simplistic safe/unsafe area label."
- **Cloud-eval harness** (reference pattern 4): a JSONL dataset (`query`/`response`/
  `ground_truth`) scored by Foundry's built-in `intent_resolution` / `relevance` /
  `groundedness` evaluators (1–5, pass ≥ 3), **plus HomeScout safety evaluators** for the
  two boundaries above. Runs `[Category("External")]`, off the blocking gate. Decide the
  eval-tool language per the reference doc's cross-cutting decision.

Evidence:

- Prompt version is linked from the feature note.
- Eval dataset and rubric exist.
- [[GenAIOps Learning Path Integration]] remains current.

## Phase 4: Streaming, Tracing, And User Trust

Purpose:

- Make AI output feel responsive and inspectable.
- Add observability before the workflow becomes hard to debug.

Watch:

- Part 5: streaming.

Microsoft Learn:

- Continue "Evaluate and optimize AI agents through structured experiments."
- Introduce only lightweight trace correlation here.
- Save the full "Analyze and debug your generative AI app with tracing" module until after the monitoring/deployment foundation in Phase 7.

Build:

- Streaming comparison/report endpoint if the backend shape supports it.
- React streaming UI for report generation.
- Correlation id on comparison requests.
- OpenTelemetry-compatible tracing around API request, prompt version, tool calls, agent run, and response.

Testing and evaluation:

- Tests for streaming contract or graceful fallback.
- Eval run comparing at least two prompt variants.
- **Batch experiment harness** (reference pattern 5): run a `test-prompts/` set against
  the agent, capture **response + token usage + latency** per answer, and write
  `experiments/{name}/…` for A/B comparison across prompt/model variants — the input to
  cost-aware iteration.
- Manual trace inspection note.

Evidence:

- Trace id is returned or visible in development diagnostics.
- Eval result is recorded.
- [[Testing Strategy]] updated if patterns change.

## Phase 5: Saved Comparisons And User Workspace

Purpose:

- Turn a one-off demo into a usable workspace.

Watch:

- Part 6: conversations.
- Part 9: user auth, if private workspaces become necessary before uploads.

Microsoft Learn:

- Continue structured experiments as needed.

Build:

- Saved comparison sessions.
- Rename course "conversation" concepts into HomeScout language.
- User-scoped saved searches when authentication is added.
- Introduce the user-owned case-file concept as the private corpus attached to a comparison or saved property search.

Testing:

- Persistence tests.
- API tests for saved comparison ownership boundaries.
- Tests that case-file metadata is scoped to the correct saved comparison or user.
- React tests for loading and switching saved comparisons where practical.

Evidence:

- Saved comparison workflow is usable locally.
- [[Component Architecture]] and [[Endpoint Summary]] updated.

## Phase 6: Real Data And Document Inputs

Purpose:

- Move from toy comparison to practical homebuying evidence.

Watch:

- Part 8: image/PDF input.
- Part 7: image generation only as an optional extension point.

Microsoft Learn:

- Continue evaluation module.
- Start "Automate AI evaluations with Microsoft Foundry and GitHub Actions" once the eval dataset is stable.

Build:

- Upload listing, EPC, survey, screenshot, or floorplan files.
- Store uploaded documents and extracted facts in the user-owned case file.
- Add retrieval over the user-owned case file for property-specific evidence.
- Add retrieval over the curated HomeScout knowledge base for stable guidance:
  - UK homebuying process explainers
  - EPC terminology
  - survey terminology
  - cost-estimation assumptions
  - crime, amenity, and school data interpretation notes
  - safety rules and "not mortgage advice" boundaries
  - source reliability guidance
- Extract structured facts behind the API.
- Add one real public-data integration at a time:
  - amenities
  - crime context
  - schools
  - Land Registry price context

Testing and evaluation:

- Fixture-based parsing tests.
- Fake HTTP-handler tests for external APIs.
- Retrieval tests for uploaded case-file evidence.
- Retrieval tests for curated knowledge-base guidance.
- Evaluation cases for missing, conflicting, or stale source data.
- **Automated eval gate** (reference pattern 6): once the hand-curated eval set (Phase 3)
  is stable, wire a workflow that runs it with **Azure OIDC** (`azure/login@v2`,
  `id-token: write` — keyless, no stored credential), posts a results summary (e.g. a PR
  comment), and stays **off the blocking gate** (scheduled/opt-in, like `external-checks`)
  so a third-party outage never blocks a merge. Least-privilege
  `permissions: { contents: read, pull-requests: write, id-token: write }`.

Evidence:

- External data source behavior is documented.
- No live network dependency in normal tests.
- Feature notes identify source freshness and limitations.

## Phase 7: Azure Deployment Management

Purpose:

- Make deployment and environment management part of the project, not an afterthought.

Watch:

- Revisit any course deployment/configuration sections as they appear.

Microsoft Learn:

- "Automate AI evaluations with Microsoft Foundry and GitHub Actions."
- "Monitor your generative AI application."
- "Analyze and debug your generative AI app with tracing."

Build:

- Azure environment plan for dev first.
- Managed identity direction.
- Secret handling through Azure-native configuration, not committed files.
- Application Insights or Azure Monitor telemetry path.
- Foundry resource configuration notes.
- Azure storage/search plan for user case files and curated knowledge retrieval.
- Deployment scripts or Azure Developer CLI flow when chosen.
- **Grow the azd/bicep infra** (reference pattern 7): HomeScout ships a **Basic** Foundry
  setup today; the reference `infra/` shows the fuller `azd` shape to converge on — App
  Insights + Log Analytics (module 5 monitoring), AI Search + storage (Standard agents /
  RAG). Add these modules incrementally as the owning features (monitoring, case-file RAG)
  land, not all at once.

Deployment management requirements:

- Separate local, dev, and future production assumptions.
- Document required Azure resources.
- Document cost-sensitive services and shutdown guidance.
- Keep deployment reproducible.

Testing and evaluation:

- CI runs unit and API tests.
- CI runs stable evaluation checks when agent behavior exists.
- Deployment smoke check verifies API status and frontend availability.

Evidence:

- Azure deployment page or section exists before the first real cloud deploy.
- Monitoring dashboard or metric list is documented.
- [[Log]] records deployment decisions and costs.

## Phase 8: Personalization, Memory, And Speech

Purpose:

- Add richer interaction only after core comparison, evaluation, and deployment discipline exist.

Watch:

- Part 10: user memory and personalization.
- Part 11: speech input.

Microsoft Learn:

- Continue monitoring.
- Continue tracing.

Build:

- Buyer preferences.
- Search priorities.
- Spoken viewing notes.
- Memory update rules that the user can inspect or change.
- Add buyer preferences and spoken viewing notes to the user-owned case file only when privacy and inspection rules are clear.

Testing and evaluation:

- Preference persistence tests.
- Evaluation cases for personalization overreach.
- Privacy and data-retention notes.

Evidence:

- Personalization behavior is explainable.
- User can understand what was remembered and why it affected a response.

## Default Weekly Loop

Use this loop for each course/video chunk:

1. Watch the next video.
2. Inspect matching companion repo files.
3. Read the matching Microsoft Learn module section when it applies.
4. Update or confirm the video note.
5. Build the HomeScout translation.
6. Add tests or evaluations before moving to the next AI behavior.
7. Update wiki pages and log.
8. Commit code and docs together.

## What To Do First

The next recommended sequence is:

1. Phase 1 design pass on the React workspace.
2. Watch Part 1 and Part 2 with the current HomeScout scaffold open.
3. Finish the GenAIOps "Plan and prepare" module.
4. Build the comparison workspace shell and API status integration.
5. Watch Part 3.
6. Build the first deterministic cost-estimate tool and tests.
7. Start prompt governance only after the API/tool boundary is real.
