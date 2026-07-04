// HomeScout Copilot — reproducible Foundry provisioning (azd + bicep).
//
// Minimal "basic agent" set for the conversational cost-answer slice:
//   Foundry account (AIServices) -> chat model deployment -> Foundry project -> RBAC.
// Cosmos (agent thread storage), AI Search, and Document Intelligence are deferred to
// their phases (persistence / RAG), exactly as RagLab layers them.
//
// AUTHORED + compiles (`az bicep build`); NOT yet `azd up`-verified — provisioning is
// verified by running `azd up` with Azure credentials (seam-first: this is the real
// dependency the offline work stands in for).

targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the azd environment; drives resource names and the resource group.')
param environmentName string

@minLength(1)
@description('Primary Azure region for all resources.')
param location string

@description('Object ID of the deploying user (azd sets AZURE_PRINCIPAL_ID). Granted Foundry access for local dev. Empty to skip.')
param principalId string = ''

@description('Chat model deployment name — a stable role label the app passes as the model, decoupled from the underlying model.')
param chatDeploymentName string = 'chat'

@description('Chat model name. A current, tool-calling model with GlobalStandard quota in eastus2. (Earlier gpt-4.1-mini / gpt-4o-mini versions became deprecated and are blocked for new deployments — verified against the live model catalog on 2026-07-04.)')
param chatModelName string = 'gpt-5-mini'

@description('Chat model version.')
param chatModelVersion string = '2025-08-07'

@description('Model deployment SKU. GPT-5-family models are offered on GlobalStandard/DataZoneStandard, not regional Standard; gpt-5-mini has GlobalStandard quota in eastus2.')
@allowed(['Standard', 'GlobalStandard', 'DataZoneStandard'])
param chatSku string = 'GlobalStandard'

var tags = {
  'azd-env-name': environmentName
  project: 'homescout-copilot'
}
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

resource rg 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module account 'modules/foundry-account.bicep' = {
  scope: rg
  name: 'foundry-account'
  params: {
    location: location
    tags: tags
    accountName: 'aif-${resourceToken}'
    chatDeploymentName: chatDeploymentName
    chatModelName: chatModelName
    chatModelVersion: chatModelVersion
    chatSku: chatSku
  }
}

// Project is a separate module that depends on the account (via its output), so it is
// created only after the account + model deployment settle — RagLab's mitigation for
// the "resource entity provisioning state is not terminal" RequestConflict.
module project 'modules/foundry-project.bicep' = {
  scope: rg
  name: 'foundry-project'
  params: {
    location: location
    tags: tags
    accountName: account.outputs.accountName
    projectName: 'proj-${resourceToken}'
    deployerPrincipalId: principalId
  }
}

// Cloud evaluation store (ADLS Gen2) for Microsoft.Extensions.AI.Evaluation reporting — persisted,
// regression-tracked answer-quality results + cached judge responses. Independent of the account,
// so it settles in parallel.
module evalStorage 'modules/eval-storage.bicep' = {
  scope: rg
  name: 'eval-storage'
  params: {
    location: location
    tags: tags
    storageAccountName: 'steval${resourceToken}'
    deployerPrincipalId: principalId
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_FOUNDRY_ACCOUNT string = account.outputs.accountName
output AZURE_FOUNDRY_PROJECT_ENDPOINT string = project.outputs.projectEndpoint
output AZURE_FOUNDRY_MODEL_DEPLOYMENT string = chatDeploymentName
output AZURE_FOUNDRY_JUDGE_DEPLOYMENT string = account.outputs.judgeDeploymentName
output AZURE_EVAL_STORAGE_ENDPOINT string = evalStorage.outputs.dfsEndpoint
output AZURE_EVAL_STORAGE_FILESYSTEM string = evalStorage.outputs.fileSystemName
