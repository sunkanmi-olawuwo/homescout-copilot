# Overview

HomeScout Copilot is an AI homebuying due-diligence assistant. It helps users compare properties and neighbourhoods using public data, uploaded documents, saved preferences, and conversational analysis.

The MVP focuses on property and area comparison. It does not provide regulated mortgage advice or recommend specific financial products.

## Purpose

Homebuyers often need to combine scattered information: listings, price history, schools, crime, commute, amenities, running costs, and personal preferences. HomeScout gives that work a guided workspace.

Example questions:

- "Compare this SE10 listing with this CR0 listing for commute, schools, parks, crime, and monthly costs."
- "What should I ask at the viewing based on this floorplan and EPC?"
- "Which area better fits my budget and commute preferences?"

## Tech Stack

- .NET 10
- .NET Aspire
- React
- TypeScript
- Vite
- ASP.NET Core API service
- xUnit test project
- Planned Microsoft Agent Framework integration
- Planned Azure OpenAI integration

## Current Architecture

The repo currently uses the Aspire starter shape:

- `HomeScoutCopilot.AppHost` coordinates local app hosting.
- `HomeScoutCopilot.ServiceDefaults` provides shared Aspire defaults.
- `frontend` hosts the React/Vite frontend.
- `HomeScoutCopilot.ApiService` is the service/API layer for future data and AI tools.
- `HomeScoutCopilot.Tests` is the integration test project.

See [[Component Architecture]] for project boundaries.

## Safety Boundary

HomeScout can provide:

- estimates
- comparisons
- public-data summaries
- caveats
- questions to ask professionals
- decision-support context

HomeScout must not provide:

- regulated mortgage advice
- specific mortgage product recommendations
- definitive property valuations
- unsupported claims that an area is safe or unsafe

## Course Alignment

The project follows the YouTube playlist "Let's build a Chatbot [AI in C#]" and maps each generic chatbot feature into a HomeScout-specific feature. See [[Course Playlist Tracker]].

