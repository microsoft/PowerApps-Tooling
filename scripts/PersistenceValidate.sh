#!/bin/bash

# This script is used to validate whether persistence has valid tests

cd bin/Debug/YamlValidator

APP_TEST_DIR="../Persistence.Tests/_TestData/AppsWithYaml"
CONTROL_TEST_DIR="../Persistence.Tests/_TestData/ValidYaml-CI"

app_test_results=$(dotnet YamlValidator.dll validate --path $APP_TEST_DIR)
printf "Validating Directory $APP_TEST_DIR \n"
printf "$app_test_results"

control_test_results=$(dotnet YamlValidator.dll validate --path $CONTROL_TEST_DIR)
printf "Validating Directory $CONTROL_TEST_DIR \n"
printf "$control_test_results"
