# Part 4: Reasoning

## Course Implementation

Status: Planned.

The course extracts reasoning text from the AI response and renders it separately.

## Companion Repo Code

Primary files:

- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/AssistantReasoningResponse.razor`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/MidTurnVisuals.razor`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/ChatbotPage.razor.cs`

## HomeScout Translation

Add an explainable comparison panel. It should show why HomeScout is making a comparison point without exposing unsafe or overly verbose chain-of-thought.

Use product language:

- evidence notes
- assumptions
- source context
- tradeoff explanation

## Implementation Checklist

- [ ] Add an evidence/tradeoff panel to comparison results.
- [ ] Show assumptions for cost estimates.
- [ ] Show source labels for public-data summaries when available.
- [ ] Avoid exposing raw chain-of-thought.
- [ ] Update [[Frontend Design Guidelines]] if the evidence UI pattern evolves.

## Divergence

Adapt. HomeScout should show useful evidence and assumptions, not internal reasoning as a product feature.

