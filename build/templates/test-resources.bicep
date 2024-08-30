// Define the location for the deployment of the components.
param location string

// Define the name of the storage account that will be created.
param storageAccountName string

// Define the name of the CosmosDb database account that will be created.
param cosmosDbName string

// Define the name of the CosmosDb MongoDb database that will be created.
param cosmosDb_mongoDb_databaseName string

// Define the name of the CosmosDb NoSql database that will be created.
param cosmosDb_noSql_databaseName string

// Define the name of the key vault where the necessary secrets will be stored to access the deployed test resources.
param keyVaultName string

// Define the Service Principal ID that needs access full access to the deployed resource group.
param servicePrincipal_objectId string

module storageAccount 'br/public:avm/res/storage/storage-account:0.9.1' = {
  name: 'storageAccountDeployment'
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

module cosmosDb 'br/public:avm/res/document-db/database-account:0.6.0' = {
  name: 'cosmosDeployment'
  params: {
    name: cosmosDbName
    location: location
    enableFreeTier: true
    capabilitiesToAdd: [
      'EnableMongo'
      'EnableServerless'
    ]
    mongodbDatabases: [
      {
        name: cosmosDb_mongoDb_databaseName
      }
    ]
    sqlDatabases: [
      {
        name: cosmosDb_noSql_databaseName
      }
    ]
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'DocumentDB Account Contributor'
      }
    ]
  }
}

module vault 'br/public:avm/res/key-vault/vault:0.6.1' = {
  name: 'vaultDeployment'
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
