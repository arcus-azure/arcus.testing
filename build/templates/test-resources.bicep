// Define the location for the deployment of the components.
param location string

// Define the name of the Azure Data Factory resource that will be created.
param dataFactoryName string

// Define the name of the storage account that will be created.
param storageAccountName string

// Define the name of the Cosmos DB for MongoDB database account that will be created.
param cosmosDbMongoDbName string

// Define the name of the Cosmos DB for MongoDB database that will be created.
param cosmosDbMongoDbDatabaseName string

// Define the name of the Cosmos DB for NoSQL database account that will be created.
param cosmosDbNoSqlName string

// Define the name of the Cosmos DB for NoSQL database that will be created.
param cosmosDbNoSqlDatabaseName string

// Define the name of the Service Bus namespace resource that will be created.
param serviceBusNamespaceName string

// Define the name of the Key Vault where the necessary secrets will be stored to access the deployed test resources.
param keyVaultName string

// Define the service principal ID that needs access full access to the deployed resource group.
param servicePrincipalObjectId string

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
        principalId: servicePrincipalObjectId
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
      }
      {
        principalId: servicePrincipalObjectId
        roleDefinitionIdOrName: 'Storage Table Data Contributor'
      }
    ]
  }
}

module cosmosDb_mongoDb 'br/public:avm/res/document-db/database-account:0.6.0' = {
  name: 'cosmosMongoDbDeployment'
  params: {
    name: cosmosDbMongoDbName
    location: location
    enableFreeTier: true
    capabilitiesToAdd: [
      'EnableMongo'
      'EnableServerless'
    ]
    mongodbDatabases: [
      {
        name: cosmosDbMongoDbDatabaseName
      }
    ]
    roleAssignments: [
      {
        principalId: servicePrincipalObjectId
        roleDefinitionIdOrName: 'DocumentDB Account Contributor'
      }
    ]
  }
}


module cosmosDb_noSql 'br/public:avm/res/document-db/database-account:0.6.0' = {
  name: 'cosmosNoSqlDeployment'
  params: {
    name: cosmosDbNoSqlName
    location: location
    disableLocalAuth: false
    capabilitiesToAdd: [
      'EnableServerless'
    ]
    sqlDatabases: [
      {
        name: cosmosDbNoSqlDatabaseName
      }
    ]
    roleAssignments: [
      {
        principalId: servicePrincipalObjectId
        roleDefinitionIdOrName: 'DocumentDB Account Contributor'
      }
    ]
    sqlRoleAssignmentsPrincipalIds: [
      servicePrincipalObjectId
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

module serviceBusNamespace 'br/public:avm/res/service-bus/namespace:0.10.1' = {
  name: 'serviceBusNamespaceDeployment'
  params: {
    name: serviceBusNamespaceName
    location: location
    enableTelemetry: false
    publicNetworkAccess: 'Enabled'
    skuObject: {
      name: 'Standard'
    }
    zoneRedundant: false
    roleAssignments: [
      {
        principalId: servicePrincipalObjectId
        roleDefinitionIdOrName: 'Azure Service Bus Data Owner'
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
        principalId: servicePrincipalObjectId
        roleDefinitionIdOrName: 'Key Vault Secrets officer'
      }
    ]
    secrets: [
    ]
  }
}
