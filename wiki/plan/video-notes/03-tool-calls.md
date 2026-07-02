# Part 3: Tool Calls

## Course Implementation

Status: Planned.

The course explores agent tool calling in the Blazor Server chatbot.

## Companion Repo Code

Primary commits/files:

- `5030b72 WIP`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/ChatbotPage.razor.cs`
- `src/ChatBot.BlazorServerOnly/Extensions/FunctionCallContentExtensions.cs`
- `src/ChatBot.BlazorServerOnly/Extensions/FunctionResultContentExtensions.cs`
- `src/ServiceDefaults/SecretKeys.cs`
- `src/AppHost/AppHost.cs`

## HomeScout Translation

Add HomeScout data tools. Start with a deterministic cost-estimate tool before live API integrations.

Recommended first tool:

- `estimate_monthly_costs(price, deposit, rate, termYears, serviceCharge, groundRent)`

Then add tool interfaces for:

- crime summary
- amenities lookup
- school context
- price-paid context

## Implementation Checklist

- [ ] Define a tool abstraction or service boundary in the API/service layer.
- [ ] Implement monthly ownership cost estimator.
- [ ] Add tests for the estimator.
- [ ] Wire the tool into the chatbot/agent flow once agent packages are introduced.
- [ ] Update [[Endpoint Summary]], [[Testing Strategy]], and [[Feature Coverage]].

## Divergence

Adapt. The companion repo uses weather/image examples; HomeScout uses property due-diligence tools.

