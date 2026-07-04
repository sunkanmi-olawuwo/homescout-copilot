// Foundry (AI Services) account + one chat model deployment.

@description('Azure region.')
param location string

@description('Tags applied to all resources.')
param tags object

@description('Globally-unique AI Services account name (also the custom subdomain).')
param accountName string

param chatDeploymentName string
param chatModelName string
param chatModelVersion string

@description('Model deployment SKU. Regional Standard has default quota in common regions; GlobalStandard often needs a quota grant.')
@allowed(['Standard', 'GlobalStandard', 'DataZoneStandard'])
param chatSku string = 'Standard'

@description('Capacity in thousands of tokens/min.')
param chatCapacity int = 30

resource account 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    // A custom subdomain is required for Entra ID (token) auth and project endpoints.
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
    // Required to create Foundry projects under this AIServices account (the new Foundry
    // project model). Without it, project creation fails with BadRequest.
    allowProjectManagement: true
    // Prefer Entra ID; local dev + the app both use DefaultAzureCredential. Set true in
    // production to force Entra-only.
    disableLocalAuth: false
  }
}

resource chat 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: account
  name: chatDeploymentName
  sku: {
    name: chatSku
    capacity: chatCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
  }
}

output accountName string = account.name
