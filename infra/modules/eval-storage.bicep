// Evaluation store — an ADLS Gen2 storage account for the Microsoft.Extensions.AI.Evaluation
// reporting library (AzureStorageReportingConfiguration), giving cloud-persisted, regression-tracked
// evaluation results + cached judge responses. Keyless: data-plane access is Entra-only.

@description('Azure region.')
param location string

@description('Tags applied to all resources.')
param tags object

@description('Storage account name (ADLS Gen2, globally unique, 3-24 lowercase alphanumerics).')
param storageAccountName string

@description('Blob filesystem (container) that holds the evaluation store.')
param fileSystemName string = 'evaluations'

@description('Deployer object id (azd principal) — granted Storage Blob Data Contributor for local dev. Empty to skip.')
param deployerPrincipalId string = ''

// Storage Blob Data Contributor — data-plane read/write to blobs/paths via Entra ID. Required by
// the DataLakeServiceClient the evaluation reporting library uses (no account keys).
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    isHnsEnabled: true // ADLS Gen2 (hierarchical namespace) — required by AzureStorageReportingConfiguration.
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false // keyless — force Entra ID data-plane auth, least privilege.
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  parent: storage
  name: 'default'
}

resource fileSystem 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  parent: blobService
  name: fileSystemName
}

resource deployerBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(deployerPrincipalId)) {
  name: guid(storage.id, deployerPrincipalId, storageBlobDataContributorRoleId)
  scope: storage
  properties: {
    principalId: deployerPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalType: 'User'
  }
}

output storageAccountName string = storage.name
output dfsEndpoint string = storage.properties.primaryEndpoints.dfs
output fileSystemName string = fileSystem.name
