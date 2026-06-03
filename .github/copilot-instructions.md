# Copilot Instructions for PowerApps-Tooling

## Build and Test

This is a .NET SDK project (SDK 10.0, pinned in `global.json`). The solution is `src/PASopa.sln`.

```shell
# Build
dotnet build src/PASopa.sln

# Run all tests (Windows only — includes net48 targets)
dotnet test --no-build --solution src/PASopa.sln

# Run a specific test project
dotnet test --no-build --project src/Persistence.Tests/Persistence.Tests.csproj
dotnet test --no-build --project src/PAModelTests/PAModelTests.csproj

# Run a single test by name
dotnet test --no-build --project src/Persistence.Tests/Persistence.Tests.csproj --filter "FullyQualifiedName~MsappArchiveTests.SomeTestName"

# Run tests for a specific target framework
dotnet test --no-build --project src/Persistence.Tests/Persistence.Tests.csproj --framework net10.0

# Clean repo (removes all build artifacts except src/.vs)
scorch.cmd
```

Multi-targeting: Projects target `net8.0`, `net10.0`, and `net48`. Building `net48` requires Windows with .NET Framework 4.8 Developer Pack. On Linux/macOS, `net48` targets are skipped automatically.

Warnings are treated as errors at the command line (`TreatWarningsAsErrors` is set in `Directory.Build.props`), but suppressed in VS IDE to avoid blocking development flow.

## Architecture

### Two library generations

- **Persistence** (`src/Persistence/`): The active library (`Microsoft.PowerPlatform.PowerApps.Persistence`). Handles the modern `.msapp` format with YAML-based source representation (`.pa.yaml` files). This is where new work happens. The 'Source Code' schema of canvas YAML (aka PaYamlV3).
- **PAModel** (`src/PAModel/`): Legacy library (`Microsoft.PowerPlatform.Formulas.Tools`) for the older PASopa pack/unpack tool. No longer actively developed, although some maintenance may still occur. The 'Experimental' schema of canvas YAML (aka PaYamlV1).

### Persistence library structure

- **`Compression/`** — Cross-platform archive abstraction (`PaArchive`, `PaArchiveEntry`, `PaArchivePath`) with both sync and async extract APIs.
- **`MsApp/`** — Archive model: `MsappArchive` wraps `ZipArchive` to read/write `.msapp` files. `MsappLayoutConstants` centralizes all archive entry paths. Factory pattern via `IMsappArchiveFactory`.
- **`MsappPacking/`** — Core pack/unpack orchestration: `MsappPackingService` converts between `.msapp` archives and unpacked source directory structures. Unpack validates version constraints (`MinSupportedMSAppStructureVersion`, `MinSupportedDocVersion`) before extracting. Pack reads `.msapr` reference files and `Src/**/*.pa.yaml` to rebuild archives.
- **`PaYaml/`** — YAML serialization layer: `PaYamlSerializer` is the central entry point, built on YamlDotNet. Uses strongly-typed models like `NamedObject<T>` for YAML document structure. The models in `src\Persistence\PaYaml\Models\SchemaV3\**` represent the 'Source Code' schema of canvas YAML (PaYamlV3). These models should match the schema defined in `src/schemas/pa-yaml/v3.0/` and are used for both serialization and deserialization.
- **`TfmAdapters/` and `TfmExtensions/`** — Polyfills and adapter shims so modern C# features work on `net48` (using PolySharp).
- **`Extensions/`** — Helper extension methods for JSON, LINQ, and strings.

### Dependency injection

Services are registered via `IServiceCollection` extension methods using `TryAddSingleton`:
- `services.AddMsappArchiveFactory()` — registers `IMsappArchiveFactory`
- `services.AddMsappPackingService()` — registers `MsappPackingService` and its dependencies

### Error handling

`PersistenceLibraryException` with categorized `PersistenceErrorCode` (1xxx = System, 2xxx = Serialization, 3xxx = Deserialization, 4xxx = Archive). Exceptions carry optional context: `MsappEntryFullPath`, `LineNumber`, `Column`, `JsonPath`.

### Schemas

JSON schemas for the `.pa.yaml` format live in `src/schemas/pa-yaml/` (versioned: `v3.0`). Published schemas go to the root `schemas/` directory via `src/schemas/publish.ps1`.

## `YamlValidator` project

This project only worked with PaYamlV2, which has been deprecated. It's only here for historical reference and as a starter in case we want to repurpose it to validate PaYamlV3.

## Conventions

### Testing

- Test framework is **MSTest** via MSTest.Sdk (configured in `global.json`).
- Test classes inherit from `TestBase` (which extends `VSTestBase`) for shared helpers like `JsonShouldBeEquivalentTo()`, `ToJsonElement()`, and test output folder management.
- Serialization tests inherit from `SerializationTestBase`.
- Assertions use **FluentAssertions** (pinned ≤7.x due to license — see `Directory.Packages.props`).
- Test data lives in `_TestData/` directories within test projects, organized by scenario (`ValidYaml/`, `InvalidYaml/`, `AppsWithYaml/`, etc.).
- `Persistence.Testing` project provides shared test utilities (`CapturingLogger<T>`, `FilePathComparer`).

### Package version constraints

Several packages have upper version limits due to license changes — these are documented with `WARNING` comments in `src/Directory.Packages.props`:
- FluentAssertions: ≤7.x
- JsonSchema.Net: ≤8.0.5
- Yaml2JsonNode: ≤2.4.0

### Code style

- C# language version: 13.0 (set in Persistence csproj)
- Nullable reference types: enabled
- File-scoped namespaces (e.g., `namespace Foo;`)
- Primary constructors used for DI (e.g., `MsappPackingService(IMsappArchiveFactory, ...)`)
- Root namespace: `Microsoft.PowerPlatform.PowerApps.Persistence` for Persistence, `Microsoft.PowerPlatform.Formulas.Tools` for PAModel
- Copyright header: `// Copyright (c) Microsoft Corporation.` + `// Licensed under the MIT License.`
- Central package management via `src/Directory.Packages.props`
- Global usings for `System`, `System.Collections.Generic`, `System.Diagnostics` defined in `src/Directory.Build.props`
- InternalsVisibleTo for test assemblies is configured with signing key in `Directory.Build.targets`
