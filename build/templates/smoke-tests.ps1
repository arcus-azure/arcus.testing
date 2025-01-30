BeforeAll {
  $clientSecret = ConvertTo-SecureString $env:servicePrincipalKey -AsPlainText -Force
  $pscredential = New-Object -TypeName System.Management.Automation.PSCredential($env:servicePrincipalId, $clientSecret)
  Connect-AzAccount -ServicePrincipal -Tenant $env:tenantId -Credential $pscredential
}

Describe 'Storage account' {
  BeforeEach {
    $storageContext = New-AzStorageContext -StorageAccountName $env:ARCUS_TESTING_STORAGEACCOUNT_NAME -UseConnectedAccount
  }
  It 'Service principal can get blob container' {
    Get-AzStorageContainer -Context $storageContext
  }
  It 'Service principal can create blob container' {
    $containerName = 'test-container'
    try {
      New-AzStorageContainer -Name $containerName -Context $storageContext
    } finally {
      Remove-AzStorageContainer -Name $containerName -Context $storageContext -Force
    }
  }
  It 'Service principal can create Cosmos MongoDb collection' {
    $collectionName = 'test-collection'
    try {
      New-AzCosmosDBMongoDBCollection `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -AccountName $env:ARCUS_TESTING_COSMOS_MONGODB_NAME `
        -DatabaseName $env:ARCUS_TESTING_COSMOS_MONGODB_DATABASENAME `
        -Name $collectionName
    }
    finally {
      Remove-AzCosmosDBMongoDBCollection `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -AccountName $env:ARCUS_TESTING_COSMOS_MONGODB_NAME `
        -DatabaseName $env:ARCUS_TESTING_COSMOS_MONGODB_DATABASENAME `
        -Name $collectionName
    }
  }
  It "Service principal can create Cosmos NoSql container" {
    $containerName = 'test-container'
    try {
      New-AzCosmosDBSqlContainer `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -AccountName $env:ARCUS_TESTING_COSMOS_NOSQL_NAME `
        -DatabaseName $env:ARCUS_TESTING_COSMOS_NOSQL_DATABASENAME `
        -Name $containerName `
        -PartitionKeyPath '/pk' `
        -PartitionKeyKind Hash
    }
    finally {
      Remove-AzCosmosDBSqlContainer `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -AccountName $env:ARCUS_TESTING_COSMOS_NOSQL_NAME `
        -DatabaseName $env:ARCUS_TESTING_COSMOS_NOSQL_DATABASENAME `
        -Name $containerName
    }
  }
  It "Service principal can create Service bus queue" {
    $queueName = 'test-queue'
    try {
      New-AzServiceBusQueue `
          -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
          -NamespaceName $env:ARCUS_TESTING_SERVICEBUS_NAMESPACE `
          -Name $queueName
    } finally {
      Remove-AzServiceBusQueue `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -NamespaceName $env:ARCUS_TESTING_SERVICEBUS_NAMESPACE `
        -Name $queueName
    }
  }
  It "Service principal can create Service bus topic" {
    $topicName = 'test-topic'
    try {
      New-AzServiceBusTopic `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -NamespaceName $env:ARCUS_TESTING_SERVICEBUS_NAMESPACE `
        -Name $topicName
    } finally {
      Remove-AzServiceBusTopic `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -NamespaceName $env:ARCUS_TESTING_SERVICEBUS_NAMESPACE `
        -Name $topicName
    }
  }
}