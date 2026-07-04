# Part 9: User Auth

## Course Implementation

Status: Planned.

The course adds user authentication using Microsoft Entra ID.

## Companion Repo Code

Primary commits/files:

- `ae82169 Auth Added`
- `b896229 WIP`
- `7457cde Ready`
- `src/ChatBot.BlazorServerOnly/Program.cs`
- `src/ChatBot.BlazorServerOnly/Components/Layout/LoginDisplay.razor`
- `src/ChatBot.BlazorServerOnly/Components/RedirectToLogin.razor`
- `src/ChatBot.BlazorServerOnly/Components/Routes.razor`
- `src/ChatBot.BlazorServerOnly/Extensions/ClaimsPrincipalExtensions.cs`
- `src/ChatBot.BlazorServerOnly/Services/ConversationsService.cs`
- `src/ChatBot.BlazorServerOnly/Services/FileUploadStorageService.cs`

## HomeScout Translation

Add private buyer workspaces.

Authenticated data should include:

- saved comparisons
- uploaded property artifacts
- buyer preferences
- viewing notes

## Implementation Checklist

- [ ] Configure auth approach.
- [ ] Add login/logout UI.
- [ ] Scope saved comparisons to user ID.
- [ ] Scope attachments to user ID.
- [ ] Add safety review for unauthenticated access.
- [ ] Update [[Component Architecture]], [[Endpoint Summary]], and [[Testing Strategy]].

## Divergence

**Diverge from the course's Entra ID: HomeScout end-user sign-in will use Keycloak** (OIDC),
following the RagLab precedent — see [[Plan Divergence]] (2026-07-04). Azure *resource* access is
unchanged (Entra managed identity / `DefaultAzureCredential`); this is only about end-user login and
per-user data. User-facing language should be HomeScout workspace/auth language, not IdP-specific.

Threads/conversation can start **anonymous** (session-scoped) with no auth; Keycloak is only needed
for cross-device / per-user history.

