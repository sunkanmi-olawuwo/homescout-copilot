# Course Playlist Tracker

Last refreshed: 2026-07-02  
Playlist: https://www.youtube.com/playlist?list=PLhGl0l5La4sZDg1isoXWnFGXBXDyM42sQ  
RSS feed: https://www.youtube.com/feeds/videos.xml?playlist_id=PLhGl0l5La4sZDg1isoXWnFGXBXDyM42sQ  
Companion repo: https://github.com/rwjdk/chatbot  
Companion repo state checked: `062d953 Part 11 Done`

Use this table to keep the course and HomeScout implementation aligned.

| Part | Video | Watched | Course Feature | HomeScout Mapping | Companion Code | Status |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | Let's build a Chatbot in C# using Microsoft Agent Framework | No | Series vision | HomeScout product vision and scope | `README.md` | Planned |
| 1 | Repo + Aspire Solution | No | Companion repo and Aspire solution | HomeScout Aspire scaffold/config alignment | `src/AppHost`, `src/ServiceDefaults`, commit `421bf76` | In progress |
| 2 | Blazor Server Baseline | No | Basic Blazor chatbot | Property and area comparison workspace | `ChatbotPage.razor`, `ChatbotPage.razor.cs` | Planned |
| 3 | Tool Calls | No | Agent tool calling | Cost estimator first, then public-data tools | `ChatbotPage.razor.cs`, function-call extensions, `SecretKeys.cs` | Planned |
| 4 | Reasoning | No | Extract/display reasoning text | Evidence notes, assumptions, and tradeoff panel | `AssistantReasoningResponse.razor`, `MidTurnVisuals.razor` | Planned |
| 5 | Streaming | No | Streaming responses | Live area/property report generation | `23e696a`, `MidTurnVisuals.razor` | Planned |
| 6 | Conversations | No | Save and restore conversations | Saved comparisons and property searches | `Conversation.cs`, `ConversationsService.cs`, `LeftSidebar.*` | Planned |
| 7 | Image Generation | No | Generate images | Defer; keep report-visual extension point | `ImageGenerationTool.cs`, `TaskType.cs`, `ImageGenStyle.cs` | Deferred for MVP |
| 8 | Image/PDF Input | No | Upload image/PDF files | Upload listings, EPCs, surveys, floorplans | `cd6bc60`, attachment/storage/mapper files | Planned |
| 9 | User Auth | No | Microsoft Entra ID auth | Private buyer workspace and user-scoped storage | `ae82169`, auth components, claims extension | Planned |
| 10 | User Memory and Personalization | No | User memory | Buyer preferences and search priorities | `6e16737`, personalization provider/service/models | Planned |
| 11 | Speech Input | No | Browser recording and transcription | Viewing voice notes and spoken search criteria | `062d953`, `chatbotAudioRecorder.js` | Planned |

## Update Routine

When new videos are released:

1. Check the RSS feed.
2. Add the video to this table as `Watched = No`.
3. Create a note in `wiki/plan/video-notes/`.
4. Add the companion code files/commits after inspecting the repo.
5. Watch the video.
6. Record the course implementation.
7. Record the HomeScout translation.
8. Implement the HomeScout version.
9. Commit the note and implementation together.

## Roadmap

The detailed implementation sequence lives in [[Video Implementation Roadmap]].

## Release Monitoring

Use [[Release Monitoring]] before each course session to check for new playlist videos and companion repo commits.

## Divergence Rule

If HomeScout intentionally differs from the course implementation, record the reason in [[Plan Divergence]].

