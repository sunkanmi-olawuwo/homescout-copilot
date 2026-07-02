# Plan Divergence

Use this page whenever implementation differs from:

- the course companion repo
- a playlist step
- an existing product plan
- an existing wiki page

## Entries

### 2026-07-02: Aspire Starter Instead Of Exact Companion Repo Shape

Course/plan expectation:

- Track the course and companion repo closely.

Actual implementation:

- Created HomeScout with `dotnet new aspire-starter`, which includes Blazor web, API service, AppHost, ServiceDefaults, and tests.

Reason:

- The starter gives a working product-oriented scaffold while staying close to the course's Aspire direction.

Impact:

- Some starter pages such as Counter and Weather exist temporarily.
- These should be replaced as each course feature maps into HomeScout.

### 2026-07-02: Wiki Canonical Plan Root

Previous state:

- Plans lived under `docs/`.
- A temporary compatibility layer linked `docs/` paths into `wiki/plan/`.

New state:

- Canonical plans live under `wiki/plan/`.
- The `docs/` compatibility layer has been removed.
- Plan divergence checks should read `wiki/plan/` directly.

Reason:

- This is a solo project, so duplicate documentation paths add friction without helping coordination.
- The project now uses `wiki/` as the single development memory.

