# infra/

Reproducible Foundry provisioning (azd + bicep) for HomeScout Copilot.

Provisions the minimal "basic agent" set for the conversational cost-answer slice:
**Foundry account (AIServices) → chat model deployment → Foundry project → RBAC.**
Cosmos (agent thread storage), Azure AI Search, and Document Intelligence are deferred
to their phases (persistence / RAG), as RagLab layers them.

## Provision

Requires the Azure CLI + Azure Developer CLI (`azd`) and an Azure subscription.

    azd auth login
    azd env new homescout-dev          # choose a name
    azd env set AZURE_LOCATION eastus
    azd provision                      # (or: azd up)

`azd` writes the outputs — project endpoint and model deployment name — into the
environment's `.env` (`AZURE_FOUNDRY_PROJECT_ENDPOINT`, `AZURE_FOUNDRY_MODEL_DEPLOYMENT`).
A later slice reads them as `FoundryOptions`.

## Status & notes

- The bicep **compiles** (`az bicep build`, enforced by `infra-ci.yml`) but is **not yet
  `azd up`-verified** — provisioning is proven by running `azd provision` with Azure
  credentials (per "verify, don't assume").
- **Model:** defaults to `gpt-4.1-mini` (default Standard quota in common regions).
  Override via `chatModelName` / `chatModelVersion` / `chatSku`; `GlobalStandard` models
  (e.g. gpt-5-*) may need a quota grant.
- The **project is created after the account settles** (separate module) — RagLab's
  mitigation for the "provisioning state is not terminal" conflict.
- **RBAC:** the deployer gets the **Foundry User** role for local-dev agent access; the
  app's managed-identity assignment is added when the API is deployed as a service.
