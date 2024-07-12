Describe 'Storage account' {
  BeforeEach {
    $clientSecret = ConvertTo-SecureString $env:servicePrincipalKey -AsPlainText -Force
    $pscredential = New-Object -TypeName System.Management.Automation.PSCredential($env:servicePrincipalId, $clientSecret)
    Connect-AzAccount -ServicePrincipal -Tenant $env:tenantId -Credential $pscredential
    
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
}