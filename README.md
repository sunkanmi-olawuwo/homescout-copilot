# HomeScout Copilot

HomeScout Copilot is an AI homebuying due-diligence assistant built with .NET, Aspire, React, and Microsoft Foundry Agent Service patterns.

The app helps a buyer compare properties and areas using public data, uploaded documents, and conversational analysis. The MVP focuses on property and area comparison, not regulated mortgage advice.

## Start Here

The project wiki is the canonical development memory:

- `wiki/index.md` - catalog of all wiki pages
- `wiki/overview.md` - purpose, stack, architecture, and safety boundary
- `wiki/onboarding-article.md` - narrative guide for returning to the project
- `wiki/__plans/00-roadmap/course/course-playlist-tracker.md` - course-to-HomeScout implementation tracker
- `wiki/frontend-design-guidelines.md` - binding frontend design direction

Plan files live under `wiki/__plans/`, with `wiki/` as the only documentation home.

## Product Direction

HomeScout answers questions like:

- How does this postcode compare with another one?
- What are the nearby schools, transport links, parks, supermarkets, and health services?
- What does recent crime data suggest about the area?
- How do monthly ownership costs compare between two property prices?
- What should I ask during a viewing based on this listing or survey PDF?

## Course-Aligned Build Strategy

This project follows the YouTube playlist and companion repo step by step, but every course feature is translated into HomeScout product behavior.

Playlist: https://www.youtube.com/playlist?list=PLhGl0l5La4sZDg1isoXWnFGXBXDyM42sQ  
Companion repo: https://github.com/rwjdk/chatbot

For each video:

1. Record what the course implemented.
2. Decide how that feature maps to HomeScout.
3. Implement the HomeScout version.
4. Update relevant wiki pages.
5. Commit the notes and implementation together.

## Current Scaffold

.NET code lives under `dotnet/` (solution: `dotnet/HomeScoutCopilot.slnx`):

- `dotnet/src/HomeScoutCopilot.AppHost`: Aspire app host.
- `dotnet/src/HomeScoutCopilot.ServiceDefaults`: Aspire service defaults.
- `dotnet/src/HomeScoutCopilot.ApiService`: API/service layer for HomeScout data tools.
- `dotnet/tests/HomeScoutCopilot.Tests`: NUnit test project (contract + Aspire integration).
- `frontend`: React/Vite frontend (at the repo root).

Build and test: `dotnet test dotnet/HomeScoutCopilot.slnx`. Run all quality-gate
checks at once with `scripts/quality-gate.sh`.

## Safety Boundary

HomeScout is a decision-support tool. It can provide estimates, comparisons, summaries, and questions to investigate. It must not recommend a specific regulated mortgage product or present itself as financial advice.

## Planned Data Sources

- Police.uk API for street-level crime data.
- HM Land Registry open data for price paid and house price context.
- GOV.UK school performance data for nearby schools.
- OpenStreetMap/Overpass for amenities.
- TfL Unified API for London transport and commute analysis.

