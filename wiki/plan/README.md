# Plan Overview

This folder contains the project plans, course mapping, and divergence records.

Canonical plan files live here. Legacy paths under `docs/` should point to these files so older workflows and plan-comparison tools can still find the current plan without creating duplicates.

## Files

- [[Product Brief]] - Product scope, MVP, target user, and success criteria.
- [[Course Playlist Tracker]] - Course video to HomeScout implementation mapping.
- [[Plan Divergence]] - Record of implementation decisions that diverge from a plan or course implementation.
- `video-notes/` - Per-video notes with course implementation, HomeScout translation, product decision, and implementation notes.

## Plan Divergence Tool Compatibility

Use `wiki/plan/` as the canonical plan root.

If a tool still expects old `docs/` paths, keep those paths as symlinks to the matching wiki files:

- `docs/product-brief.md` -> `../wiki/plan/product-brief.md`
- `docs/playlist-tracker.md` -> `../wiki/plan/course-playlist-tracker.md`
- `docs/video-notes` -> `../wiki/plan/video-notes`

Do not maintain duplicate copies.

