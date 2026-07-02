# HomeScout Copilot

HomeScout Copilot is an AI homebuying due-diligence assistant built with .NET, Aspire, Blazor, and Microsoft Agent Framework patterns.

The app helps a buyer compare properties and areas using public data, uploaded documents, and conversational analysis. The MVP focuses on property and area comparison, not regulated mortgage advice.

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
4. Commit the notes and implementation together.

## Current Scaffold

- `HomeScoutCopilot.AppHost`: Aspire app host.
- `HomeScoutCopilot.ServiceDefaults`: Aspire service defaults.
- `HomeScoutCopilot.Web`: Blazor frontend.
- `HomeScoutCopilot.ApiService`: API/service layer for HomeScout data tools.
- `HomeScoutCopilot.Tests`: integration test project.

## Safety Boundary

HomeScout is a decision-support tool. It can provide estimates, comparisons, summaries, and questions to investigate. It must not recommend a specific regulated mortgage product or present itself as financial advice.

## Planned Data Sources

- Police.uk API for street-level crime data.
- HM Land Registry open data for price paid and house price context.
- GOV.UK school performance data for nearby schools.
- OpenStreetMap/Overpass for amenities.
- TfL Unified API for London transport and commute analysis.

