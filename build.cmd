@pushd %~dp0
@dotnet run --project "./targets/targets.csproj" -- %*
@popd
