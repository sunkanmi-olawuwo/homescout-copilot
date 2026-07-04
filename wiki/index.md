# Wiki Index

This wiki is the canonical development memory for HomeScout Copilot.

## Core Pages

- [[Overview]] - High-level product purpose, stack, architecture, and safety boundary.
- [[Component Architecture]] - Application projects, components, ownership boundaries, and current scaffold.
- [[Endpoint Summary]] - API routes, integrations, and planned data flow.
- [[API-First Foundry Agents]] - Architecture decision for API-first backend boundaries and Microsoft Foundry Agent Service.
- [[Coding Conventions]] - Repository conventions for C#, React, TypeScript, docs, Git, and frontend discipline.
- [[Testing Strategy]] - Test framework, expected coverage, and verification patterns.
- [[Feature Coverage]] - Implemented, scaffolded, planned, and deferred features.
- [[Frontend Design Guidelines]] - Strict frontend design direction for HomeScout UI work.
- [[RAG Architecture]] - Case-file RAG and curated HomeScout knowledge-base design for agentic retrieval.
- [[Accelerator Product Direction]] - Product strategy for taking HomeScout toward an accelerator-ready renter/buyer due-diligence copilot.
- [[Onboarding Article]] - Narrative guide for returning to the project and understanding what exists.
- [[Log]] - Chronological record of sessions and decisions.

## Plan Pages

- [[HomeScout Plans]] - How plans are organized (`wiki/__plans/`, numbered phase folders) and how course videos map into implementation work.
- [[HomeScout Skeleton Migration]] - Master sequenced plan for restructuring HomeScout to the RagLab skeleton.
- [[Product Brief]] - Product scope, MVP, target user, and success criteria.
- [[Course Playlist Tracker]] - Per-video course-to-HomeScout mapping with companion code references.
- [[GenAIOps Learning Path Integration]] - Maps Microsoft Learn GenAIOps modules into HomeScout build artifacts, evals, monitoring, and tracing.
- [[GenAIOps Reference Implementation]] - Concrete, adoptable patterns from Microsoft's official GenAIOps lab repo (`mslearn-genaiops`) — versioned agents, prompt manifest, cloud evals, batch experiments, CI eval gate — mapped to HomeScout phases.
- [[Phased Learning And Build Plan]] - Step-by-step sequence for videos, Microsoft Learn modules, design, build work, tests, evaluations, and Azure deployment.
- [[Work Tracks]] - The two parallel tracks (frontend / backend) and the API seam between them, so multiple agents can work concurrently without collisions.
- [[GenAIOps Tooling Plan]] - The AgentOps (deploy) + Evaluator (evaluation) .NET tool projects.
- [[Frontend Implementation Plan]] - Review the design brief + design-agent deliverables, then implement the React app against the API.
- [[Readiness Checklist]] - Current starting gate for implementation sessions and next immediate steps.
- [[Release Monitoring]] - How to check for new playlist videos and companion repo commits.
- [[Video Implementation Roadmap]] - Detailed plan for every currently available playlist video.
- [[Plan Divergence]] - Record of places where implementation diverges from plans or course material.

## Raw Sources

- `wiki/raw/source-inventory.md` - Immutable record of original sources used to seed the project.

## Maintenance Rules

- Add new pages here when they are created.
- Keep one-line summaries current.
- Use `[[Page Name]]` links in prose and normal Markdown links only when linking to exact files or external URLs.
