// Define the location for the deployment of the components.
param location string

// Define the name of the resource group where the components will be deployed.
param resourceGroupName string

// Define the name of the storage account that will be created.
param storageAccountName string

// Define the name of the key vault where the necessary secrets will be stored to access the deployed test resources.
param keyVaultName string

// Define the Service Principal ID that needs access full access to the deployed resource group.
param servicePrincipal_objectId string

targetScope = 'subscription'

module resourceGroup 'br/public:avm/res/resources/resource-group:0.2.3' = {
  name: 'resourceGroupDeployment'
  params: {
    name: resourceGroupName
    location: location
  }
}

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' existing = {
  name: resourceGroupName
}

module storageAccount 'br/public:avm/res/storage/storage-account:0.9.1' = {
  name: 'storageAccountDeployment'
  scope: rg
  params: {
    name: storageAccountName
    location: location
    allowBlobPublicAccess: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
      }
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Storage Table Data Contributor'
      }
    ]
  }
}

module vault 'br/public:avm/res/key-vault/vault:0.6.1' = {
  name: 'vaultDeployment'
  dependsOn: [
    resourceGroup
  ]
  scope: rg
  params: {
    name: keyVaultName
    location: location
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Key Vault Secrets officer'
      }
    ]
    secrets: [
    ]
  }
}
