![image](https://img.shields.io/github/workflow/status/microsoft/PowerApps-Language-Tooling/CI/master)  ![image](https://img.shields.io/nuget/vpre/Microsoft.PowerPlatform.Formulas.Tools)

# Power Apps Source File Pack and Unpack Utility

**This project is still in Experimental - format may change**
 
**We welcome feedback on the project, file format, and capabilities.** 

This tool enables Canvas apps to be edited outside of Power Apps Studio and managed in a source control tool such as GitHub.  The basic work flow is:
1. Download an existing Canvas app as a .msapp file, using **File** > **Save as** > **This computer** in Power Apps Studio.
1. Use this tool to extract the .msapp file into editable source files.
1. Edit these files with any text editor.
1. Check these files into any source control manager.
1. Use this tool to recreate a .msapp file from the editable source files.
1. Upload the .msapp file using **File** > **Open** > **Browse** in Power Apps Studio.

This is similar to the [Solution Packager](https://docs.microsoft.com/en-us/power-platform/alm/solution-packager-tool) for Microsoft Dataverse.

## Power Platform CLI usage

To get started, download and install the [Microsoft Power Platform CLI](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/powerapps-cli).

To unpack a .msapp file: `pac canvas unpack --msapp FromApp.msapp --sources ToSourceFolder`
To pack a .msapp file: `pac canvas pack --msapp ToApp.msapp --sources FromSourceFolder`

### Versioning

The output files (ie, "yaml version") have a version number. During preview, the tool is not backwards compatible, so the version used to re-pack must match the version used to pack.  When unpacking, keep a copy of the original msapp so that you can unpack it with future versions of the tool. 

| Pac CLI Version | Yaml version |
| --- | --- |
| 1.7.2 | 0.23 | 
| 1.7.4 | 0.23 | 

Latest Yaml version is: https://github.com/microsoft/PowerApps-Language-Tooling/blob/0a4c9e624ffad1f93b6c085aff029945a0fbc00a/src/PAModel/Serializers/SourceSerializer.cs#L49 



## Test app usage

You can also use this functionality stand alone, using our test console app.  

Download and install the [.NET Core SDK v3.1.x (x64)](https://dotnet.microsoft.com/download/dotnet-core/3.1) in order to build.
Build the test console app by running: `\build.cmd`  
This will create: `\bin\Debug\PASopa\PASopa.exe`

To unpack a .msapp file: `pasopa -unpack FromApp.msapp ToSourceFolder`
To pack a .msapp file: `pasopa -pack ToApp.msapp FromSourceFolder`

## Folder structure
Unpack and pack use this folder structure:

- **\src** - the control and component files. This contains the sources.
   - \*.pa.yaml - the formulas extracted from the control.json file.  **This is the place to edit your formulas.**- 
   - CanvasManifest.json - a manifest file. This contains what is normally in the header, properties, and publishInfo.
   - \*.json - the raw control.json file.
   - \EditorState\*.editorstate.json - cached information for Studio to use.
- **\DataSources** - all data sources used by the app.
- **\Connections** - connection instances saved with this app and used when reloading into studio. 
- **\Assets** - media files embedded in the app.
- **\pkgs** - A downloaded copy of external references, such as templates, API Definition files, and component libaries. These are similar to nuget/npm references. 
- **\other** - all miscellaneous files needed to recreate the .msapp
   - entropy.json - volatile elements (like timestamps) are extracted to this file. This helps reduce noisy diffs in other files while ensuring that we can still round trip.
   - Holds other files from the msapp, such as what is in \references

## File format
The .pa.yaml files use a subset of [YAML](https://yaml.org/spec/1.2/spec.html).  Most notably and similar to Excel, all expressions must begin with an `=` sign.  More details are available [here](/docs/YAMLFileFormat.md)

## Merging changes from Studio
When merging changes made in two different Studio sessions:
- Ensure that all control names are unique.  It is easy for them not to be, as inserting a button in two different sessions can easily result in two `Button1` controls.  We recommend naming controls soon after creating them to avoid this problem.  The tool will not accept two controls with the same name.  
- Ensure  variable and collection names don't conflict. These won't have merge conflicts, but they can have semantic conflicts. 
- For these files, merge them as you normally would:
	- \src\*.fx.yaml 
- If there are conflicts or errors, you can delete these files:
	- \src\editorstate\*.json  - These files contain optional information in studio (such as whether a control is locked for editing). 
	- \Entropy\*  - this includes checksum.json and entropy.json. 
	- \Connections\* - these files save per-org connection information. Deleting them is similar to "logging out".  Note for security purposes, these files  *don't* include the actual login tokens, they just include a guid pointing into the environment where the login tokens are stored. 
- If there are any merge conflicts under these paths, it is not safe to merge.   Let us know if this happens often and we will work on restructuring the file format to avoid conflicts.   However, it is safe to add whole files when they don't conflict. 
	- \DataSources\*
	- \pkgs\*
	- CanvasManifest.json 

When merging multiple apps together into a single app:
- Be sure to merge App.OnStart formulas together. 

## Contributing

We welcome feedback on the design, file format, and capabilities. Comments and issues are very welcome.   

*This project is still experimental and we routinely refactor the folder structure, file format, and implementation in big ways.  As such, we aren't yet accepting code contributions until we are more stable.*

Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit [https://cla.opensource.microsoft.com](https://cla.opensource.microsoft.com).

Before making a Pull Request, please file an Issue and solicit discussion. 

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### Setting up a dev box

For a developer machine (Windows 10, WSL, Linux, macOS), install:

- [git](https://git-scm.com/downloads)
- [.NET Core SDK v3.1.x (x64)](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [VS Code](https://code.visualstudio.com/Download)
- if on Windows: [VS2019 (Community edition will do)](https://visualstudio.microsoft.com/downloads/).  Select at least the following workload: .NET Core cross-plat
- recommended VSCode extensions:
  - [GitLens (eamodio.gitlens)](https://github.com/eamodio/vscode-gitlens)
  - [C# (ms-vscode.csharp)](https://github.com/OmniSharp/omnisharp-vscode)

### Building and running tests

After cloning this repo (https://github.com/microsoft/PowerApps-Language-Tooling), open a terminal/cmd/PS prompt with the dotnet executable on the path. Check with: ```dotnet --version ```

To build, run tests and produce nuget packages, run this command:

```bash
./build ci
```

To list all build targets, run: ```./build --list-tree```

To see other build help, run: ```./build --help```
