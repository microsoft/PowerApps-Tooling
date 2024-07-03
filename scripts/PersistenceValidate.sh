#!/bin/bash

# This script is used to validate whether persistence has valid tests

cd ../bin/Debug/YamlValidator

appTestResults=$(dotnet YamlValidator.dll validate --path "../Persistence.Tests/_TestData/AppsWithYaml")
echo "$appTestResults"

controlTestResults=$(dotnet YamlValidator.dll validate --path "../Persistence.Tests/_TestData/ValidYaml-CI")
echo "$controlTestResults"

