# Part 8: Image/PDF Input

## Course Implementation

Status: Planned.

The course refactors conversation handling to support image and PDF input.

## Companion Repo Code

Primary commit/files:

- `cd6bc60 Part 008 done`
- `src/ChatBot.BlazorServerOnly/Models/ConversationAttachment.cs`
- `src/ChatBot.BlazorServerOnly/Services/FileUploadStorageService.cs`
- `src/ChatBot.BlazorServerOnly/Services/ConversationChatMessageMapper.cs`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/ChatbotPage.razor`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/ChatbotPage.razor.cs`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/UserMessage.razor`
- `src/ChatBot.BlazorServerOnly/Program.cs`

## HomeScout Translation

Support uploaded property artifacts:

- property listing PDFs
- EPC PDFs
- survey reports
- floorplans
- listing screenshots
- viewing photos

## Implementation Checklist

- [ ] Add attachment model for HomeScout comparison sessions.
- [ ] Add upload UI that matches [[Frontend Design Guidelines]].
- [ ] Validate file type and size.
- [ ] Store uploads safely and user-scope them once auth exists.
- [ ] Map files into AI messages or extraction pipeline.
- [ ] Update [[Endpoint Summary]], [[Testing Strategy]], and [[Feature Coverage]].

## Divergence

Adapt. The course asks "what is in this image"; HomeScout asks "what should I know about this property artifact?"

