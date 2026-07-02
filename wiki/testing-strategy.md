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
- core Blazor component state where practical

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

