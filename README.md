
This takes a Canvas App (.msapp file) and converts to and from text files that can be checked into source control.
This is similar to the "SolutionPackager" for CDS.

This aggressively ensures the msapps can faithfully roundtrip - and unpacking will immediately do a sanity test and repack and compare.

# File Format

This is the resulting folder structure after an extraction:

1. \src\ - the control and component files. This contains the sources.
   1. CanvasManifest.json - a manifest file. This contains what is normally in the header, properties, and publishInfo.
   2. *.json - the raw control.json file.
   3. *.pa1 - the scripts extracted from the control.json file.
1. \other\ - all miscellaneous files needed to recreate the .msapp
   1. entropy.json - volatile elements (like timestamps) are extracted to this file. This helps reduce noisy diffs in other files while ensuring that we can still round trip.
   2. Holds other files from the msapp, such as what is in \references
1. \DataSources\ - a file per datasource.


# Usage
There is a test console app to drive this. The official way to consume this is through the PowerApps CLI https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/powerapps-cli .

```
pasopa.exe  -unpack PathToMyApp.msapp FolderToExtractTo
```



# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# Building from source

## Setting up a dev box

For a developer machine (Windows 10, WSL, Linux, macOS), install:

- [git](https://git-scm.com/downloads)
- [dotnet Core SDK v3.1.x (x64)](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [VS Code](https://code.visualstudio.com/Download)
- if on Windows: [VS2019 (Community edition will do)](https://visualstudio.microsoft.com/downloads/)
    Select at least the following workloads:
  - .NET Core cross-plat

- recommended VSCode extensions:
  - [GitLens (eamodio.gitlens)](https://github.com/eamodio/vscode-gitlens)
  - [C# (ms-vscode.csharp)](https://github.com/OmniSharp/omnisharp-vscode)

## Building and running tests

After cloning this repo (https://github.com/microsoft/PowerApps-Language-Tooling), open a terminal/cmd/PS prompt with the
dotnet executable on the path. Check with: ```dotnet --version ```

To build, run tests and produce nuget packages, run this command:

```bash
./build ci
```

To list all build targets, run: ```./build --list-tree```

To see other build help, run: ```./build --help```
