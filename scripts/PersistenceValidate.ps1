<#
  Validate's the Yaml Controls used by Persistence's Unit Tests
#>

Set-Location -Path "bin\Debug\YamlValidator"

$appTestResults = dotnet YamlValidator.dll validate --path "..\Persistence.Tests\_TestData\AppsWithYaml"
Write-Host $appTestResults

$controlTestResults = dotnet YamlValidator.dll validate --path "..\Persistence.Tests\_TestData\ValidYaml-CI"
Write-Host $controlTestResults
