# Part 6: Conversations

## Course Implementation

Status: Planned.

The course saves and restores previous conversations and explains why the implementation does not rely on `AgentSession` for long-term storage.

## Companion Repo Code

Primary commits/files:

- `c7af012 Conversation System v1`
- `ed839b3 Much better setup`
- `f8470e4 WIP`
- `a1d37c6 Video part 1-6 done`
- `src/ChatBot.BlazorServerOnly/Models/Conversation.cs`
- `src/ChatBot.BlazorServerOnly/Models/ConversationMessage.cs`
- `src/ChatBot.BlazorServerOnly/Services/ConversationsService.cs`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/LeftSidebar.razor`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/LeftSidebar.razor.cs`

## HomeScout Translation

Model saved property comparison sessions.

Product concepts:

- saved comparison
- property search
- comparison message
- comparison artifact

## Implementation Checklist

- [ ] Decide whether to keep `Conversation` naming initially or rename to HomeScout concepts.
- [ ] Store comparison sessions.
- [ ] Add saved search sidebar.
- [ ] Add session titles based on location/listing.
- [ ] Plan durable storage beyond temp JSON.
- [ ] Update [[Component Architecture]], [[Feature Coverage]], and [[Testing Strategy]].

## Divergence

Adapt. The course saves generic conversations; HomeScout saves comparison sessions and search context.

