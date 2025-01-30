// Define the location for the deployment of the components.
param location string

// Define the name of the DataFactory resource that will be created.
param dataFactoryName string

// Define the name of the storage account that will be created.
param storageAccountName string

// Define the name of the CosmosDb MongoDb database account that will be created.
param cosmosDb_mongoDb_name string

// Define the name of the CosmosDb MongoDb database that will be created.
param cosmosDb_mongoDb_databaseName string

// Define the name of the CosmosDb NoSql database account that will be created.
param cosmosDb_noSql_name string

// Define the name of the CosmosDb NoSql database that will be created.
param cosmosDb_noSql_databaseName string

// Define the name of the Azure EventHubs namespace that will be created.
param eventHubsNamespaceName string

// Define the name of the key vault where the necessary secrets will be stored to access the deployed test resources.
param keyVaultName string

// Define the Service Principal ID that needs access full access to the deployed resource group.
param servicePrincipal_objectId string

module factory 'br/public:avm/res/data-factory/factory:0.4.0' = {
  name: 'dataFactoryDeployment'
  params: {
    name: dataFactoryName
    location: location
  }
}

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

module cosmosDb_mongoDb 'br/public:avm/res/document-db/database-account:0.6.0' = {
  name: 'cosmosMongoDbDeployment'
  params: {
    name: cosmosDb_mongoDb_name
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
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'DocumentDB Account Contributor'
      }
    ]
  }
}


module cosmosDb_noSql 'br/public:avm/res/document-db/database-account:0.6.0' = {
  name: 'cosmosNoSqlDeployment'
  params: {
    name: cosmosDb_noSql_name
    location: location
    disableLocalAuth: false
    capabilitiesToAdd: [
      'EnableServerless'
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
    sqlRoleAssignmentsPrincipalIds: [
      servicePrincipal_objectId
    ]
    sqlRoleDefinitions: [
      {
        name: 'MetadataRole'
        roleType: 'CustomRole'
        dataAction: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/listKeys/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
        ]
      }
    ]
    backupPolicyContinuousTier: null
    backupPolicyType: null
    backupStorageRedundancy: null
  }
}

module eventHubsNamespace 'br/public:avm/res/event-hub/namespace:0.9.1' = {
  name: 'eventHubsNamespaceDeployment'
  params: {
    name: eventHubsNamespaceName
    location: location
    skuName: 'Basic'
    skuCapacity: 1
    publicNetworkAccess: 'Enabled'
    roleAssignments: [
      {
        principalId: servicePrincipal_objectId
        roleDefinitionIdOrName: 'Event Hubs Data Owner'
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
