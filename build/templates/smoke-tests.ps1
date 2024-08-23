BeforeAll {
  $clientSecret = ConvertTo-SecureString $env:servicePrincipalKey -AsPlainText -Force
  $pscredential = New-Object -TypeName System.Management.Automation.PSCredential($env:servicePrincipalId, $clientSecret)
  Connect-AzAccount -ServicePrincipal -Tenant $env:tenantId -Credential $pscredential
}

Describe 'Storage account' {
  BeforeEach {
    $storageContext = New-AzStorageContext -StorageAccountName $env:storageAccountName -UseConnectedAccount
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
        -ResourceGroupName $env:resourceGroupName `
        -AccountName $env:cosmosDbName `
        -DatabaseName $env:cosmosDb_mongoDb_databaseName `
        -Name $collectionName
    }
    catch {
      Remove-AzCosmosDBMongoDBCollection `
        -ResourceGroupName $env:resourceGroupName `
        -AccountName $env:cosmosDbName `
        -DatabaseName $env:cosmosDb_mongoDb_databaseName `
        -Name $collectionName
        -Force
    }
  }
}