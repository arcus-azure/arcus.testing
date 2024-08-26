$definitions = az policy definition list | ConvertFrom-Json
$items = az policy state list -g 'arcus-testing-dev-we-rg' | ConvertFrom-Json

Describe 'Azure policies' {
  foreach ($item in $items) {
    It $item.policyDefinitionReferenceId {
      $definition = $definitions | where { $_.id -eq $item.policyDefinitionId }
      $item.complianceState | Should -Be 'Compliant' -Because $definition.description + $items
    }
  }
}