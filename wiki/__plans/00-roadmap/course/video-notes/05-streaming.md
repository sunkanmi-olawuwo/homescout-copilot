# Part 5: Streaming

## Course Implementation

Status: Planned.

The course handles streaming versus non-streaming AI responses in Blazor Server.

## Companion Repo Code

Primary commits/files:

- `23e696a Streaming works`
- `b79c88f WIP`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/ChatbotPage.razor.cs`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/MidTurnVisuals.razor`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/AssistantTextResponse.razor`

## HomeScout Translation

Stream area/property reports as they are generated.

Use cases:

- live area comparison
- live viewing checklist
- live tradeoff report

## Implementation Checklist

- [ ] Add streaming response state to the workspace.
- [ ] Add a streaming report component.
- [ ] Preserve non-streaming fallback behavior.
- [ ] Add UX states for running tools, streaming text, and finished reports.
- [ ] Update [[Feature Coverage]] and [[Testing Strategy]].

## Divergence

Adapt. Streaming should support report generation and analysis flow, not just chat-message polish.

