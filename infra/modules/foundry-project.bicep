// Foundry project (child of the account) + RBAC for local-dev data-plane access.

@description('Azure region.')
param location string

@description('Tags applied to all resources.')
param tags object

@description('Name of the existing AI Services (Foundry) account.')
param accountName string

@description('Foundry project name.')
param projectName string

@description('Deployer object id (azd principal) — granted Foundry User on the account for local dev. Empty to skip.')
param deployerPrincipalId string = ''

// Foundry User (formerly "Azure AI User") — data-plane access to Foundry projects and
// agents via Entra ID. This is the role the app's managed identity and the developer
// need to call agents (Cognitive Services roles cover only direct model inference).
var foundryUserRoleId = '53ca6127-db72-4b80-b1b0-d745d6d5456d'

resource account 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: accountName
}

resource project 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: account
  name: projectName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: 'HomeScout Copilot'
    description: 'HomeScout Copilot agent project (conversational cost answer).'
  }
}

resource deployerFoundryUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(deployerPrincipalId)) {
  name: guid(account.id, deployerPrincipalId, foundryUserRoleId)
  scope: account
  properties: {
    principalId: deployerPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', foundryUserRoleId)
    principalType: 'User'
  }
}

output projectName string = project.name
output projectEndpoint string = project.properties.endpoints['AI Foundry API']
