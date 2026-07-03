# GenAIOps Learning Path Integration

This page maps Microsoft's "Operationalize generative AI applications (GenAIOps)" learning path into HomeScout Copilot. The goal is to make the learning path a live operating model for the project, not a detached study checklist.

Source: [Operationalize generative AI applications (GenAIOps)](https://learn.microsoft.com/en-us/training/paths/operationalize-gen-ai-apps/)

Reference implementation: [[GenAIOps Reference Implementation]] distills the path's
official lab repo (`MicrosoftLearning/mslearn-genaiops`) into concrete, adoptable patterns
(versioned agents, `agent.yaml` manifest, cloud evals, batch experiments, CI eval gate)
mapped to HomeScout's .NET stack and owning phases. Read it alongside the module audit
below.

Module-by-module source check completed: 2026-07-02.

## Why This Fits HomeScout

The playlist and companion repo teach feature construction. GenAIOps teaches production discipline: prompt versioning, agent evaluation, automation, cost monitoring, and tracing.

For HomeScout, that means every AI feature should eventually answer four questions:

- What prompt, agent, or tool version produced this answer?
- How do we know the answer is useful, grounded, and safe?
- What did it cost in latency, tokens, and external-service calls?
- Can we debug the flow when an area comparison or recommendation looks wrong?

## Learning Track Shape

Run this path as a parallel track beside the video roadmap:

- Course video work decides what feature we are building.
- GenAIOps work decides how that feature becomes measurable, repeatable, observable, and maintainable.
- Wiki updates connect both: each video note should include a "GenAIOps hook" when the feature touches prompts, agents, tools, retrieval, evaluation, monitoring, or tracing.

Use [[Phased Learning And Build Plan]] for the concrete order of when to watch each video, when to do each Microsoft Learn module, and when to build.

## Module-By-Module Audit

This section records the actual module structure checked from Microsoft Learn so the HomeScout plan is grounded in the full path, not only the path summary.

### 1. Plan and prepare a GenAIOps solution

Units checked:

- Introduction
- Explore use cases for GenAIOps
- Select the right generative AI model
- Understand the development lifecycle of a language model application
- Explore available tools and frameworks to implement GenAIOps
- Exercise - Compare language models from the model catalog
- Module assessment
- Summary

HomeScout implication:

- Do this before deep feature work. It informs product scope, model choice, architecture boundaries, and why HomeScout should stay code-first and reproducible.

### 2. Manage prompts for agents in Microsoft Foundry with GitHub

Units checked:

- Introduction
- Apply version control to prompts
- Understand Microsoft Foundry agents and prompt versioning
- Organize prompts in GitHub repositories
- Develop safe prompt deployment workflows
- Exercise - Develop prompt and agent versions
- Knowledge check
- Summary

HomeScout implication:

- Prompt files, agent instructions, and deployment notes should enter the repo before the first serious Foundry-backed comparison workflow.

### 3. Evaluate and optimize AI agents through structured experiments

Units checked:

- Introduction
- Design evaluation experiments
- Apply Git-based workflows to optimization experiments
- Apply evaluation rubrics for consistent scoring
- Exercise - Evaluate and compare AI agent versions
- Knowledge check
- Summary

HomeScout implication:

- Create evaluation rubrics and a small dataset as soon as HomeScout has a prompt-driven comparison response.

### 4. Automate AI evaluations with Microsoft Foundry and GitHub Actions

Units checked:

- Introduction
- Understand why automated evaluations matter
- Align evaluators with human criteria
- Create evaluation datasets
- Implement batch evaluations with Python
- Integrate evaluations into GitHub Actions
- Exercise - Set up automated evaluations
- Knowledge check
- Summary

HomeScout implication:

- Automate only after the first hand-curated eval set is stable. CI should run ordinary tests first, then stable AI evaluation checks.

### 5. Monitor your generative AI application

Units checked:

- Introduction
- Why do you need to monitor?
- Understand key metrics to monitor
- Explore how to monitor with Azure
- Integrate monitoring into your app
- Interpret monitoring results
- Exercise - Enable monitoring for a generative AI application
- Knowledge check
- Summary

HomeScout implication:

- Azure monitoring belongs in the deployment-management phase, with latency, throughput, token usage, and error-rate metrics documented before cloud usage grows.

### 6. Analyze and debug your generative AI app with tracing

Units checked:

- Introduction
- Why do you need to use tracing?
- Identify what to trace in generative AI applications
- Implement tracing in generative AI applications
- Debug complex workflows with advanced tracing patterns
- Make informed decisions with trace data analysis
- Exercise - Enable tracing for a generative AI application
- Knowledge check
- Summary

HomeScout implication:

- Add basic correlation ids early, but complete tracing work after monitoring foundations are in place. Trace spans should cover API requests, model/agent calls, deterministic tools, external data calls, and response assembly.

## HomeScout Module Mapping

### Plan and prepare a GenAIOps solution

HomeScout artifact:

- Add a short AI system design record for the first agent workflow.
- Define the first production-like scenario: "compare two properties and explain area tradeoffs."
- Record service boundaries in [[API-First Foundry Agents]], [[Component Architecture]], and [[Endpoint Summary]].

Implementation direction:

- Keep React as the client.
- Keep `HomeScoutCopilot.ApiService` as the only frontend-facing API.
- Put Foundry agent calls, deterministic tools, data-source clients, and prompt orchestration behind backend boundaries.

Recruiter signal:

- Shows architecture thinking before model experimentation.

### Manage prompts for agents in Microsoft Foundry with GitHub

HomeScout artifact:

- Create a prompt inventory under version control.
- Track prompt purpose, owner, inputs, expected output shape, safety notes, and linked evaluations.
- Treat prompt changes like code changes.

Implementation direction:

- Store prompts in a structured repo folder once the first real agent is added.
- Use semantic prompt names such as `property-comparison-summary` and `area-risk-context`.
- Link prompt files from feature notes and tests.

Recruiter signal:

- Demonstrates prompt governance instead of one-off prompt tinkering.

### Evaluate and optimize AI agents through structured experiments

HomeScout artifact:

- Create evaluation datasets for realistic homebuying questions.
- Define rubrics for groundedness, usefulness, safety, format adherence, cost, and latency.
- Compare prompt/model/tool variants through repeatable experiments.

Implementation direction:

- Start with hand-curated examples:
  - two similar homes with different commute and school tradeoffs
  - cheaper home in an area with higher running-cost risk
  - listing with missing or ambiguous facts
- Add synthetic examples only after the core rubric is stable.

Recruiter signal:

- Shows evidence-based AI iteration.

### Automate AI evaluations with Microsoft Foundry and GitHub Actions

HomeScout artifact:

- Add an evaluation workflow that can run locally first, then in GitHub Actions.
- Fail or warn on regressions in safety, answer shape, or factual grounding.

Implementation direction:

- Keep early evaluation scripts small and repo-local.
- Run deterministic tool tests before agent evaluations.
- Use CI for stable evaluation sets once the agent boundary is real.

Recruiter signal:

- Shows continuous quality control for AI behavior.

### Monitor your generative AI application

HomeScout artifact:

- Define operational metrics for every agent workflow.
- Track latency, token usage, tool-call counts, error rates, cache hits, and estimated per-comparison cost.

Implementation direction:

- Surface observability through backend telemetry first.
- Add user-facing performance only when it helps the actual product experience.
- Keep cost notes in [[Feature Coverage]] or feature-specific plan pages when features become expensive.

Recruiter signal:

- Shows cost-aware AI engineering.

### Analyze and debug your generative AI app with tracing

HomeScout artifact:

- Add distributed tracing around API request, agent run, prompt version, tool calls, external data calls, and final response.
- Make traces useful for explaining why a comparison was produced.

Implementation direction:

- Use OpenTelemetry-compatible traces through the backend.
- Correlate each comparison response with a trace id.
- Log enough metadata to debug flow behavior without storing sensitive user input unnecessarily.

Recruiter signal:

- Shows production debugging discipline for agentic workflows.

## How This Changes Each Video Note

When a watched video touches AI behavior, update that video note with:

- Course implementation: what the playlist built.
- HomeScout implementation: what we built instead.
- GenAIOps hook: prompt, evaluation, monitoring, tracing, or CI impact.
- Evidence: test, eval result, trace screenshot, or metric added.

## First Concrete Adoption Steps

1. Add a first backend endpoint for property comparison orchestration.
2. Define the first deterministic tool: monthly ownership cost estimate.
3. Create the first prompt/agent design record before calling Foundry.
4. Add a tiny evaluation dataset for property comparison responses.
5. Add trace/correlation id plumbing before workflows become complicated.

## Boundaries

HomeScout remains a decision-support product. It should not provide regulated mortgage advice, final property valuations, or simplistic safety labels for neighborhoods.

GenAIOps should make those boundaries testable by evaluating:

- Does the response avoid regulated financial advice?
- Does the response distinguish facts, estimates, and missing information?
- Does the response avoid reducing an area to a single unsafe/safe label?
- Does the response cite or label source categories when external data is used?
