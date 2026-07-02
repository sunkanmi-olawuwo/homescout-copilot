# Testing Strategy

## Test Projects

Current test project:

- `HomeScoutCopilot.Tests`

The template uses an Aspire-oriented test project. It should become the place for integration tests that verify the app host, web frontend, API service, and future tool endpoints.

## Current Verification State

- `dotnet restore HomeScoutCopilot.sln` succeeded on 2026-07-02.
- `dotnet list HomeScoutCopilot.sln package --vulnerable --include-transitive` returned no vulnerable packages on 2026-07-02.
- Full `dotnet build HomeScoutCopilot.sln --no-restore` was attempted, but the process became stuck in the sandbox and could not be killed due restricted process controls.

## Coverage Expectations

As features are added, test coverage should include:

- data-provider parsing and error handling
- tool input validation
- cost-estimate calculations
- safety boundary behavior
- conversation persistence
- preference/memory persistence
- upload validation
- API route behavior
- React component behavior and API client behavior where practical

## AI Evaluation Expectations

Agentic behavior should be evaluated before it becomes a major product dependency.

Start evaluations as soon as HomeScout has the first backend comparison workflow that uses a prompt or Foundry agent. Use small, hand-curated datasets first, then automate stable checks in CI after the rubric settles.

Evaluation coverage should include:

- groundedness against known property facts and source categories
- usefulness of comparison summaries
- safety boundary behavior, especially avoiding regulated mortgage advice
- avoidance of simplistic safe/unsafe area labels
- output format adherence
- latency and estimated cost
- retrieval correctness for user-owned case-file evidence
- retrieval correctness for curated HomeScout knowledge-base guidance

See [[GenAIOps Learning Path Integration]] and [[Phased Learning And Build Plan]] for when evaluations enter the build sequence.

## Test Patterns

Prefer focused tests around behavior and contracts:

- Unit tests for pure calculations and mappers.
- Service tests for provider wrappers using fake clients.
- Integration tests for API routes and Aspire wiring.
- Snapshot-like tests only when the output is stable and meaningful.

## External API Testing

External data providers should be wrapped so tests do not require live network calls.

Use:

- fixture JSON
- fake HTTP handlers
- contract examples in `wiki/raw/` if they are source documents
- cached sample responses outside `wiki/raw/` if they need to evolve

## Manual Verification

For UI work:

- run the app locally
- verify desktop and mobile layouts
- check that text does not overflow
- confirm the UI follows [[Frontend Design Guidelines]]


## React Pivot Verification

Verified on 2026-07-02 after replacing the Blazor frontend with React/Vite:

- `dotnet restore HomeScoutCopilot.sln`
- `npm install` in `frontend/`
- `npm audit fix` in `frontend/`, resulting in 0 npm vulnerabilities
- `npm run build` in `frontend/`
- `dotnet build HomeScoutCopilot.ApiService/HomeScoutCopilot.ApiService.csproj --no-restore`
- `dotnet build HomeScoutCopilot.AppHost/HomeScoutCopilot.AppHost.csproj --no-restore`
- `dotnet test HomeScoutCopilot.Tests/HomeScoutCopilot.Tests.csproj --no-restore`
- `npm audit`, resulting in 0 vulnerabilities
- `dotnet list <dotnet-project>.csproj package --vulnerable --include-transitive` for AppHost, ApiService, ServiceDefaults, and Tests, resulting in no vulnerable packages
