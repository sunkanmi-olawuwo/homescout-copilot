# Video Implementation Roadmap

Last refreshed: 2026-07-02  
Playlist entries available: 12, intro plus Parts 1-11  
Companion repo state checked: `062d953 Part 11 Done`

This roadmap maps the available course videos and companion code to the HomeScout Copilot implementation plan.

## How To Use This Roadmap

For each video:

1. Watch the video.
2. Read the matching note in `wiki/plan/video-notes/`.
3. Inspect the listed companion files and commits.
4. Implement the HomeScout translation.
5. Update relevant wiki pages.
6. Commit the code and notes together.

Use [[Plan Divergence]] whenever HomeScout intentionally differs from the course.

## Roadmap Table

| Part | Course Video | Companion Code To Inspect | HomeScout Implementation | Decision |
| --- | --- | --- | --- | --- |
| 0 | Series intro | `README.md`, repo purpose | Confirm HomeScout safety boundary and product scope | Adapt |
| 1 | Repo + Aspire Solution | `src/AppHost`, `src/ServiceDefaults`, `src/ChatBot.BlazorServerOnly`, commit `421bf76` | Align our Aspire scaffold and config naming | Adapt |
| 2 | Blazor Server Baseline | `ChatbotPage.razor`, `ChatbotPage.razor.cs`, earlier `Home.razor` commits | Build React comparison workspace shell using the course behavior as reference | Adapt |
| 3 | Tool Calls | `ChatbotPage.razor.cs`, `FunctionCallContentExtensions.cs`, `FunctionResultContentExtensions.cs`, `SecretKeys.cs` | Add first HomeScout data tool abstractions and one local cost estimator | Adapt |
| 4 | Reasoning | `AssistantReasoningResponse.razor`, `MidTurnVisuals.razor`, response handling in `ChatbotPage.razor.cs` | Add explainable comparison notes/evidence panel | Adapt |
| 5 | Streaming | `ChatbotPage.razor.cs`, `MidTurnVisuals.razor`, streaming UI components | Stream generated area comparison reports | Adapt |
| 6 | Conversations | `Conversation.cs`, `ConversationMessage.cs`, `ConversationsService.cs`, `LeftSidebar.*` | Save and restore property comparison sessions | Adapt |
| 7 | Image Generation | `ImageGenerationTool.cs`, `TaskType.cs`, `ImageGenStyle.cs`, `AssistantImageResponse.razor`, `RightSidebar.*` | Defer full image generation; preserve extension point for report visuals | Defer initially |
| 8 | Image/PDF Input | `ConversationAttachment.cs`, `FileUploadStorageService.cs`, `ConversationChatMessageMapper.cs`, upload UI in `ChatbotPage.razor` | Upload listings, EPCs, surveys, screenshots, and floorplans | Adapt |
| 9 | User Auth | `Program.cs`, `LoginDisplay.razor`, `RedirectToLogin.razor`, `ClaimsPrincipalExtensions.cs` | Add private HomeScout workspace and user-scoped saved searches | Adapt |
| 10 | User Memory and Personalization | `PersonalizationContextProvider.cs`, `UserPersonalizationService.cs`, `UserPersonalization.cs`, `MemoryUpdate.cs` | Remember buyer preferences and search priorities | Adapt |
| 11 | Speech Input | `chatbotAudioRecorder.js`, audio transcription code in `ChatbotPage.razor.cs`, mic UI in `ChatbotPage.razor` | Capture spoken viewing notes and spoken search criteria | Adapt |

## Companion Commit Map

The companion repo does not provide one clean commit per video, so use this approximate map:

| Commit | Message | Roadmap Use |
| --- | --- | --- |
| `421bf76` | Blazor Server Only WIP | Parts 1-2 scaffold and baseline app shape |
| `23e696a` | Streaming works | Part 5 streaming |
| `c7af012` | Conversation System v1 | Part 6 conversation persistence start |
| `5030b72` | WIP | Part 3 tool-call plumbing and service setup |
| `ed839b3` | Much better setup | Part 6 conversation model refinement |
| `00db745` | WIP | Chatbot page/component split |
| `b79c88f` | WIP | Assistant response components and mid-turn visuals |
| `f8470e4` | WIP | Conversation service and component cleanup |
| `a1d37c6` | Video part 1-6 done | Stable reference for Parts 1-6 |
| `696b254` | Image Gen v1 | Part 7 image generation first pass |
| `d6e704b` | Image Gen. v2 | Part 7 image-generation mode/tool refinement |
| `cd6bc60` | Part 008 done | Part 8 image/PDF input |
| `ae82169` | Auth Added | Part 9 authentication first pass |
| `b896229` | WIP | Part 9 user-scoped storage/auth refinements |
| `7457cde` | Ready | Part 9 auth readiness/storage endpoint refinement |
| `6e16737` | Added User-memory | Part 10 memory and personalization |
| `fd55963` | Upgraded NuGets to Latest | Package refresh before Part 11 |
| `062d953` | Part 11 Done | Part 11 speech input |

## Implementation Sequence

### Phase 1: Foundation

Parts 0-2.

Goal:

- Make the React scaffold feel like HomeScout.
- Replace generic template pages with an actual comparison workspace skeleton.
- Keep AppHost, service defaults, and frontend boundaries understandable.

Expected wiki updates:

- [[Component Architecture]]
- [[Feature Coverage]]
- [[Frontend Design Guidelines]]
- [[Log]]

### Phase 2: API-First Agent And Tools

Parts 3-5.

Goal:

- Add the course's AI/chat/tool patterns behind API boundaries.
- Keep React as an API client, not the owner of agent orchestration.
- Start with a deterministic HomeScout tool before using live public APIs.
- Add explainable output and streaming report generation.
- Introduce a Foundry-oriented agent gateway abstraction.

Recommended first tool:

- `estimate_monthly_costs`

Reason:

- It is useful for HomeScout.
- It is deterministic.
- It avoids external API complexity while we learn tool calling.

Expected wiki updates:

- [[Endpoint Summary]]
- [[Testing Strategy]]
- [[Feature Coverage]]
- [[Plan Divergence]]

### Phase 3: Persistence And Workspaces

Part 6.

Goal:

- Save and restore comparison sessions.
- Rename tutorial conversation concepts into product concepts where useful.

HomeScout language:

- conversation -> comparison session
- chat title -> saved search title
- sidebar -> saved searches

Expected wiki updates:

- [[Component Architecture]]
- [[Feature Coverage]]
- [[Testing Strategy]]

### Phase 4: Multimodal Property Inputs

Parts 7-8.

Goal:

- Prioritize listing/survey/EPC/floorplan upload.
- Defer image generation until it has a clear product use.

Product choice:

- Part 8 is higher priority than Part 7 for HomeScout.
- Image generation can remain an extension point for report visuals.

Expected wiki updates:

- [[Endpoint Summary]]
- [[Feature Coverage]]
- [[Plan Divergence]]

### Phase 5: Private User Experience

Parts 9-11.

Goal:

- Add auth.
- Scope saved searches and uploads to a user.
- Remember buyer preferences.
- Add spoken viewing notes.

Expected wiki updates:

- [[Component Architecture]]
- [[Endpoint Summary]]
- [[Testing Strategy]]
- [[Feature Coverage]]

## Per-Video Commit Names

Suggested commit messages:

- `Plan course intro for HomeScout scope`
- `Map part 01 Aspire setup to HomeScout scaffold`
- `Map part 02 Blazor baseline to React comparison workspace`
- `Map part 03 tool calls to HomeScout cost tool`
- `Map part 04 reasoning to comparison evidence notes`
- `Map part 05 streaming to live area reports`
- `Map part 06 conversations to saved comparisons`
- `Map part 07 image generation to report visual extension`
- `Map part 08 file input to listing uploads`
- `Map part 09 auth to private buyer workspace`
- `Map part 10 memory to buyer preferences`
- `Map part 11 speech to viewing voice notes`

## Open Decisions

- React is the frontend from Part 1; the open question is how quickly to replace tutorial-shaped Blazor patterns with API-first React flows.
- Whether the first live public-data tool should be Police.uk crime data, OpenStreetMap amenities, or HM Land Registry price context.
- Whether generated images should ship in the MVP or stay a documented extension point.

