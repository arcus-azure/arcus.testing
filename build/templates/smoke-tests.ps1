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
  It 'Service principal can create CosmosDb MongoDb collection' {
    $collectionName = 'test-collection'
    try {
      New-AzCosmosDBMongoDBCollection `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -AccountName $env:ARCUS_TESTING_COSMOS_MONGODB_NAME `
        -DatabaseName $env:ARCUS_TESTING_COSMOS_MONGODB_DATABASENAME `
        -Name $collectionName
    }
    catch {
      Remove-AzCosmosDBMongoDBCollection `
        -ResourceGroupName $env:ARCUS_TESTING_RESOURCEGROUP_NAME `
        -AccountName $env:ARCUS_TESTING_COSMOS_MONGODB_NAME `
        -DatabaseName $env:ARCUS_TESTING_COSMOS_MONGODB_DATABASENAME `
        -Name $collectionName
    }
  }
}