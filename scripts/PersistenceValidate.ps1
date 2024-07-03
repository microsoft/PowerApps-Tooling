<#
  Validate's the Yaml Controls used by Persistence's Unit Tests
  Uses the repository's root as entry point (similar to how containers on github actions would)
#>

Set-Location -Path "bin\Debug\YamlValidator"

$appTestResults = dotnet YamlValidator.dll validate --path "..\Persistence.Tests\_TestData\AppsWithYaml"
$appTestResults

$controlTestResults = dotnet YamlValidator.dll validate --path "..\Persistence.Tests\_TestData\ValidYaml-CI"
$controlTestResults
