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

- ✅ **`azd provision`-verified (2026-07-04)** against subscription **HomeScoutPilot** in
  **eastus2**, and proven end-to-end by `FoundryAgentGatewayLiveTests` (the live agent
  called the `estimate_mortgage` tool against the real project). The bicep also compiles
  (`az bicep build`, enforced by `infra-ci.yml`).
- **Model:** `gpt-5-mini` (version `2025-08-07`) on **`chatSku = GlobalStandard`** — chosen
  against the **live** model catalog: earlier `gpt-4.1-mini` / `gpt-4o-mini` versions became
  deprecated and are **blocked for new deployments**, and GPT-5-family models are offered on
  GlobalStandard/DataZoneStandard (not regional `Standard`). `gpt-5-mini` had GlobalStandard
  quota (500k TPM) in eastus2. Re-check the catalog + quota per region before provisioning
  elsewhere (`az cognitiveservices model list -l <region>`, `... usage list -l <region>`).
- The account sets **`allowProjectManagement: true`** — required to create Foundry projects
  under the AIServices account (else project creation fails `BadRequest`).
- The **project is created after the account settles** (separate module) — RagLab's
  mitigation for the "provisioning state is not terminal" conflict.
- **RBAC:** the deployer gets the **Foundry User** role (`53ca6127-…`) for local-dev agent
  access **when `AZURE_PRINCIPAL_ID` is set** — the bicep skips it when empty. azd did **not**
  auto-populate it here, so set it before provisioning:
  `azd env set AZURE_PRINCIPAL_ID $(az ad signed-in-user show --query id -o tsv)`. (If a prior
  provision already ran, azd may report "no changes" and skip the grant — set the principal
  and grant directly: `az role assignment create --assignee <id> --role 53ca6127-db72-4b80-b1b0-d745d6d5456d --scope <account-id>`.)
  The app's managed-identity assignment is added when the API is deployed as a service.
- **Basic vs Standard:** this is the **Basic** setup — threads use Microsoft-managed
  storage. There is no Cosmos-only option; tenant-owned threads require the full
  **Standard** setup (Cosmos + Storage + AI Search + Key Vault + capability hosts + RBAC),
  added as a dedicated step later. See the backend plan and Plan Divergence.
