# Feature Coverage

This page tracks feature inventory across scaffolded, implemented, planned, and deferred work.

## Implemented

| Feature | Status | Notes |
| --- | --- | --- |
| Aspire solution scaffold | Implemented | Created from `aspire-starter`. |
| React frontend project | Implemented | Vite React project with initial HomeScout comparison workspace shell. |
| API service project | Implemented | API service exposes `/api/status` and `/api/comparison/sample`; HomeScout tools not yet implemented. |
| Test project | Implemented | Starter xUnit integration test project. |
| Wiki structure | Implemented | Canonical docs under `wiki/`. |

## Scaffolded But Not Product-Ready

| Feature | Status | Notes |
| --- | --- | --- |
| React comparison workspace shell | Scaffolded | Product layout exists, but generate/attach actions are placeholders. |
| Sample comparison API | Scaffolded | `/api/comparison/sample` exists as a placeholder, not the real comparison workflow. |

## Planned From Course Mapping

| Course Feature | HomeScout Feature | Status |
| --- | --- | --- |
| Course Blazor baseline | React property and area comparison workspace | Planned |
| Tool calls | Crime, amenities, school, price, cost tools | Planned |
| Reasoning | Explainable comparison notes and evidence trail | Planned |
| Streaming | Live report generation | Planned |
| Conversation history | Saved property searches and comparison sessions | Planned |
| Image generation | Optional report graphics or area summary visuals | Planned |
| Image/PDF input | Upload listings, EPCs, surveys, floorplans | Planned |
| User auth | Private user workspace | Planned |
| Memory | Buyer preferences and search priorities | Planned |
| Speech input | Spoken viewing notes and search criteria | Planned |
| RAG user case file | Private retrieval over listings, EPCs, surveys, floorplans, notes, messages, screenshots, photos, and preferences | Planned |
| Curated HomeScout knowledge base | Stable retrieval layer for homebuying explainers, terminology, assumptions, safety rules, and source guidance | Planned |

See [[Course Playlist Tracker]].

## Deferred

| Feature | Reason |
| --- | --- |
| Regulated mortgage product recommendations | Outside product safety boundary. |
| Definitive property valuation | Requires careful valuation model and liability controls. |
| Crime safety score | Too reductive; use contextual summaries instead. |
