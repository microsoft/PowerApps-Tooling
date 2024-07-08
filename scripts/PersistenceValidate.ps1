<#
  Validate's the Yaml Controls used by Persistence's Unit Tests
  Uses the repository's root as entry point (similar to how containers on github actions would)
#>

$AppTestDir = "..\Persistence.Tests\_TestData\AppsWithYaml"
$ControlTestDir = "..\Persistence.Tests\_TestData\ValidYaml-CI"

# come back to same directory after validation
Push-Location

Set-Location -Path "bin\Debug\YamlValidator"

$AppTestResults = dotnet YamlValidator.dll validate --path $AppTestDir
Write-Output "Validating Directory $AppTestDir `n"
$AppTestResults

$ControlTestResults = dotnet YamlValidator.dll validate --path $ControlTestDir
Write-Output "Validating Directory $ControlTestDir `n"
$ControlTestResults

# restore location
Pop-Location
