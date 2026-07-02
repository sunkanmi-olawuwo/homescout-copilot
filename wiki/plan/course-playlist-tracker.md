# Course Playlist Tracker

Playlist: https://www.youtube.com/playlist?list=PLhGl0l5La4sZDg1isoXWnFGXBXDyM42sQ  
RSS feed: https://www.youtube.com/feeds/videos.xml?playlist_id=PLhGl0l5La4sZDg1isoXWnFGXBXDyM42sQ  
Companion repo: https://github.com/rwjdk/chatbot

Use this table to keep the course and HomeScout implementation aligned.

| Part | Video | Watched | Course Feature | HomeScout Mapping | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | Let's build a Chatbot in C# using Microsoft Agent Framework | No | Series vision | HomeScout product vision and scope | Planned |  |
| 1 | Repo + Aspire Solution | No | Companion repo and Aspire solution | Aspire solution scaffold for HomeScout | In progress | Initial repo scaffold created from Aspire starter template. |
| 2 | Blazor Server Baseline | No | Basic Blazor chatbot | Property and area comparison chat workspace | Planned |  |
| 3 | Tool Calls | No | Agent tool calling | Crime, amenities, schools, price history, and cost tools | Planned |  |
| 4 | Reasoning | No | Extract/display reasoning text | Explainable comparison notes and evidence trail | Planned |  |
| 5 | Streaming | No | Streaming responses | Live area/property report generation | Planned |  |
| 6 | Conversations | No | Save and restore conversations | Saved searches and comparison sessions | Planned |  |
| 7 | Image Generation | No | Generate images | Optional report graphics or area summary visuals | Planned |  |
| 8 | Image/PDF Input | No | Upload image/PDF files | Upload listings, EPCs, surveys, and floorplans | Planned |  |
| 9 | User Auth | No | Microsoft Entra ID auth | Private user workspace and saved comparisons | Planned |  |
| 10 | User Memory and Personalization | No | User memory | Buyer preferences and search priorities | Planned |  |
| 11 | Speech Input | No | Browser audio recording and transcription | Spoken viewing notes and spoken search criteria | Planned |  |

## Update Routine

When new videos are released:

1. Check the RSS feed.
2. Add the video to this table as `Watched = No`.
3. Create a note in `wiki/plan/video-notes/`.
4. Watch the video.
5. Record the course implementation.
6. Record the HomeScout translation.
7. Implement the HomeScout version.
8. Commit the note and implementation together.

## Divergence Rule

If HomeScout intentionally differs from the course implementation, record the reason in [[Plan Divergence]].

