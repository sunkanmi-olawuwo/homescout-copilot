# 03 — Backend Plans

API, service-layer, persistence, and backend integration plans.

- [API Vertical Slices + Validated Options — Plan](./api-vertical-slice-plan.md) —
  restructure to RagLab parity (Carter + MediatR slices, validated options, Features folders).
- [Copilot Agent Gateway — Design (Foundry Agent Service)](./copilot-agent-gateway-plan.md)
  — the agent layer (the copilot); tool-calling over the deterministic tools.
- [Mortgage Cost Estimator — Design (MVP)](./cost-estimator-mortgage-plan.md) — the
  first deterministic capability (mortgage-only monthly repayment); a tool the agent calls.

No feature plans live here yet. Backend plans arrive with the layering and tooling
phases from the [master migration plan](../00-roadmap/homescout-skeleton-migration-plan.md)
and the [phased learning and build plan](../00-roadmap/phased-learning-build-plan.md).

Backend architecture direction lives in `wiki/component-architecture.md` and
`wiki/api-first-foundry-agents.md` (wiki root).
