// Define the location for the deployment of the components.
param location string

// Define the name of the resource group where the components will be deployed.
param resourceGroupName string

// Define the name of the storage account that will be created.
param storageAccountName string

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
  }
}
