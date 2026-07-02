# Onboarding Article

## What This Project Is

HomeScout Copilot is a learning-and-building project based on a C# AI chatbot course. Instead of copying the course as a generic chatbot, the project translates each lesson into a practical homebuying assistant.

The product helps a buyer compare properties and neighbourhoods. It should eventually combine chat, public data tools, uploaded documents, saved searches, and buyer preferences.

The important idea is this:

```text
Course feature -> understand the pattern -> implement the HomeScout version
```

For example, when the course adds a weather tool, HomeScout should learn the same tool-calling pattern but apply it to property intelligence, such as crime lookup, amenities, schools, price context, or ownership costs.

## What Exists Right Now

The repo currently has a clean .NET Aspire starter structure:

- `HomeScoutCopilot.AppHost` starts and connects the app pieces.
- `HomeScoutCopilot.ServiceDefaults` contains shared Aspire defaults.
- `frontend` is the React/Vite frontend.
- `HomeScoutCopilot.ApiService` is where service endpoints and data-tool APIs can grow.
- `HomeScoutCopilot.Tests` is the integration test project.

The frontend is not the final product yet. It has a HomeScout-branded starting screen, but the chat, comparison workspace, saved searches, and tools still need to be built.

## How To Resume Work

Start here:

1. Read [[Overview]].
2. Read [[Product Brief]].
3. Check [[Course Playlist Tracker]] for the next video/course step.
4. Read the matching note in `wiki/plan/video-notes/`.
5. Implement the HomeScout translation, not just the tutorial code.
6. Update [[Feature Coverage]], [[Endpoint Summary]], and [[Log]].

## How To Think About Scope

The MVP is property and area comparison, not full mortgage advice.

Good MVP behavior:

- "Compare these two postcodes."
- "Summarize nearby amenities."
- "Estimate monthly costs using these assumptions."
- "What viewing questions should I ask?"
- "What tradeoffs should I notice?"

Out-of-scope behavior:

- "This is the mortgage you should take."
- "This property is definitely worth this amount."
- "This area is safe or unsafe."

## Why The Wiki Matters

The wiki is the project memory. It lets you come back weeks later and know:

- what was built
- why it was built
- what course step caused it
- what product decision was made
- what still needs to happen

If the code changes and the wiki does not, the project memory becomes unreliable. Keep the wiki alive as you build.

