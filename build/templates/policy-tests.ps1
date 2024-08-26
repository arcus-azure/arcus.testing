$definitions = az policy definition list | ConvertFrom-Json
$items = az policy state list -g 'arcus-testing-dev-we-rg' | ConvertFrom-Json

Describe 'Azure policies' {
  foreach ($item in $items) {
    It $item.policyDefinitionReferenceId {
      Write-Host $item
      Write-Host $definitions
      $definition = $definitions | where { $_.id -eq $item.policyDefinitionId }
      Write-Host $definition
      $item.complianceState | Should -Be 'Compliant' -Because $definition.description
    }
  }
}