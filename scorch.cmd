@if "%_echo%"=="" echo off
pushd "%~dp0"
git clean -fdx -e /src/.vs
popd
