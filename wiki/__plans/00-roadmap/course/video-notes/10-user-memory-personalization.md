# Part 10: User Memory and Personalization

## Course Implementation

Status: Planned.

The course adds user memory and personalization to the Blazor Server chatbot.

## Companion Repo Code

Primary commit/files:

- `6e16737 Added User-memory`
- `src/ChatBot.BlazorServerOnly/AIContextProviders/PersonalizationContextProvider.cs`
- `src/ChatBot.BlazorServerOnly/Services/UserPersonalizationService.cs`
- `src/ChatBot.BlazorServerOnly/Models/UserPersonalization.cs`
- `src/ChatBot.BlazorServerOnly/Models/MemoryUpdate.cs`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/RightSidebar.razor`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/RightSidebar.razor.cs`
- `src/ChatBot.BlazorServerOnly/Program.cs`

## HomeScout Translation

Remember buyer preferences:

- max budget
- deposit range
- commute destination and tolerance
- school needs
- amenity preferences
- areas to avoid or prioritize
- property type
- deal-breakers

## Implementation Checklist

- [ ] Define buyer preference model.
- [ ] Add preference extraction/update flow.
- [ ] Show editable preferences in the UI.
- [ ] Avoid storing sensitive financial details without clear intent.
- [ ] Add tests for preference update behavior.
- [ ] Update [[Feature Coverage]] and [[Testing Strategy]].

## Divergence

Adapt. The course stores broad user memories; HomeScout should store explicit buyer preferences and make them reviewable.

