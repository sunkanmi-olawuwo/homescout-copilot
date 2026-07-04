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

@description('Judge model deployment name — a separate, higher-capability model for LLM-as-judge evaluation, kept distinct from the copilot generator to avoid self-judging.')
param judgeDeploymentName string = 'judge'

@description('Judge model. A newer, more capable model than the generator (gpt-5-mini). gpt-5.4-mini has DataZoneStandard quota in eastus2 and the longest support runway of the servable options.')
param judgeModelName string = 'gpt-5.4-mini'

param judgeModelVersion string = '2026-03-17'

@description('Judge deployment SKU. gpt-5.4-mini is offered on GlobalStandard/DataZoneStandard; DataZoneStandard has quota in eastus2.')
@allowed(['Standard', 'GlobalStandard', 'DataZoneStandard'])
param judgeSku string = 'DataZoneStandard'

param judgeCapacity int = 30

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

// Separate judge deployment for LLM-as-judge evaluation. Created after the chat deployment
// settles — a single account serializes deployment writes, so parallel creates hit the
// "resource entity provisioning state is not terminal" RequestConflict.
resource judge 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: account
  name: judgeDeploymentName
  dependsOn: [chat]
  sku: {
    name: judgeSku
    capacity: judgeCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: judgeModelName
      version: judgeModelVersion
    }
  }
}

output accountName string = account.name
output judgeDeploymentName string = judge.name
