# Part 1: Repo + Aspire Solution

## Course Implementation

Status: Planned.

The course introduces the companion repo and the Aspire part of the solution.

## Companion Repo Code

Primary commit/files:

- `421bf76 Blazor Server Only WIP`
- `src/AppHost/AppHost.cs`
- `src/AppHost/AppHost.csproj`
- `src/ServiceDefaults/Extensions.cs`
- `src/ServiceDefaults/SecretKeys.cs`
- `src/ServiceDefaults/ServiceDefaults.csproj`
- `src/ChatBot.BlazorServerOnly/ChatBot.BlazorServerOnly.csproj`
- `chatbot.slnx`

## HomeScout Translation

Create a HomeScout Aspire solution with:

- AppHost
- ServiceDefaults
- React/Vite frontend
- API service for data tools
- Tests

## Product Decision

Decision: Adapt.

Reason:

The Aspire solution structure fits the course and gives HomeScout a clean place for app hosting, UI, data tools, and tests.

## Implementation Checklist

- [ ] Compare our scaffold with companion `AppHost`.
- [ ] Align service names with HomeScout product language.
- [ ] Add future config placeholders for AI and public-data integrations only when needed.
- [ ] Keep `ApiService` as the likely home for data-tool endpoints.
- [ ] Update [[Component Architecture]] after any scaffold changes.

## Implementation Notes

- Initial scaffold was created with `dotnet new aspire-starter`, then pivoted to the Aspire React starter shape.
- Restore has been verified.
- Full solution build still needs verification outside the stuck sandbox build session.

