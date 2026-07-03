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

@description('Chat model name. Defaults to a model with default Standard quota in common regions.')
param chatModelName string = 'gpt-4.1-mini'

@description('Chat model version.')
param chatModelVersion string = '2025-04-14'

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

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_FOUNDRY_ACCOUNT string = account.outputs.accountName
output AZURE_FOUNDRY_PROJECT_ENDPOINT string = project.outputs.projectEndpoint
output AZURE_FOUNDRY_MODEL_DEPLOYMENT string = chatDeploymentName
