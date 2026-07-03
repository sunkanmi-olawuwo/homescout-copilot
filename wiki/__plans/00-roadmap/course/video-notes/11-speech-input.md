# Part 11: Speech Input

## Course Implementation

Status: Planned.

The course adds speech input through browser audio recording and transcription.

## Companion Repo Code

Primary commit/files:

- `062d953 Part 11 Done`
- `src/ChatBot.BlazorServerOnly/wwwroot/chatbotAudioRecorder.js`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/ChatbotPage.razor`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/ChatbotPage.razor.cs`
- `src/ChatBot.BlazorServerOnly/wwwroot/app.css`

## HomeScout Translation

Add voice capture for viewing notes and spoken search criteria.

Example uses:

- "I just viewed the property. The street was quiet, but the kitchen needs work."
- "Compare this place with the one near London Bridge, but schools matter more."
- "Remember that I prefer parks within a ten-minute walk."

## Implementation Checklist

- [ ] Add voice input UI to the composer.
- [ ] Reuse or adapt browser recorder module.
- [ ] Transcribe audio into the prompt or viewing note field.
- [ ] Mark voice-derived text so users can review before saving.
- [ ] Add privacy caveat for recorded/transcribed audio.
- [ ] Update [[Endpoint Summary]], [[Feature Coverage]], and [[Testing Strategy]].

## Divergence

Adapt. The course uses speech as chat input; HomeScout should support both search criteria and post-viewing notes.

