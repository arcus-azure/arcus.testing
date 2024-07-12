BeforeAll {
  $clientSecret = ConvertTo-SecureString $env:servicePrincipalKey -AsPlainText -Force
  $pscredential = New-Object -TypeName System.Management.Automation.PSCredential($env:servicePrincipalId, $clientSecret)
  Connect-AzAccount -ServicePrincipal -Tenant $env:tenantId -Credential 
}

Describe 'Storage account' {
  It 'Service principal has access to storage account' {
    Get-AzStorageContainer -ResourceGroupName $env:resourceGroupName
  }
}