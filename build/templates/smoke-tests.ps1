BeforeAll {
  $clientSecret = ConvertTo-SecureString $env:servicePrincipalKey -AsPlainText -Force
  $pscredential = New-Object -TypeName System.Management.Automation.PSCredential($env:servicePrincipalId, $clientSecret)
  Connect-AzAccount -ServicePrincipal -Tenant $env:tenantId -Credential 
}

Describe 'Storage account' {
  It 'Service principal can get blob container' {
    Get-AzStorageContainer -ResourceGroupName $env:resourceGroupName
  }
  It 'Service principal can create blob container' {
    $containerName = 'test-container'
    try {
      New-AzStorageContainer -ResourceGroupName $env:resourceGroupName -Name $containerName
    } finally {
      Remove-AzStorageContainer -ResourceGroupName $env:resourceGroupName -Name $containerName -Force
    }
  }
}