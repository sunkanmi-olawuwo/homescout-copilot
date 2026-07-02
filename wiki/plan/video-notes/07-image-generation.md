# Part 7: Image Generation

## Course Implementation

Status: Planned.

The course adds image generation to the chatbot, including routing between normal chat and image generation.

## Companion Repo Code

Primary commits/files:

- `696b254 Image Gen v1`
- `d6e704b Image Gen. v2`
- `src/ChatBot.BlazorServerOnly/Tools/ImageGenerationTool.cs`
- `src/ChatBot.BlazorServerOnly/Models/TaskType.cs`
- `src/ChatBot.BlazorServerOnly/Models/ImageGenStyle.cs`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/AssistantImageResponse.razor`
- `src/ChatBot.BlazorServerOnly/Components/Pages/Chatbot/Components/RightSidebar.razor`

## HomeScout Translation

Defer full image generation for the MVP unless it supports a concrete HomeScout workflow.

Possible later uses:

- generated report cover image
- visual area summary card
- stylized moving checklist graphic

## Implementation Checklist

- [ ] Watch and understand the tool/routing pattern.
- [ ] Document why image generation is deferred.
- [ ] Preserve an extension point for generated report visuals.
- [ ] Do not prioritize image generation over uploads and public-data tools.
- [ ] Update [[Plan Divergence]] if deferring implementation.

## Divergence

Defer initially. Image generation is technically useful to learn, but listing/PDF input is more valuable for HomeScout's MVP.

