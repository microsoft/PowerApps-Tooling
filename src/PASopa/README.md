# Legacy Source File Pack and Unpack Utility (PASopa)

> [!WARNING]
> **PASopa is an experimental tool and is not recommended for use in production environments**

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

Easiest way to get pac cli is to use [dotnet tool install](https://www.nuget.org/packages/Microsoft.PowerApps.CLI.Tool):

```
dotnet tool install --global Microsoft.PowerApps.CLI.Tool
```

To unpack a .msapp file:
```
pac canvas unpack --msapp FromApp.msapp --sources ToSourceFolder
```

To pack a .msapp file:
```
pac canvas pack --msapp ToApp.msapp --sources FromSourceFolder
```

### Versioning

The output files (ie, "yaml version") have a version number. During preview, the tool is not backwards compatible, so the version used to re-pack must match the version used to pack.  When unpacking, keep a copy of the original msapp so that you can unpack it with future versions of the tool.

| Pac CLI Version | Yaml version |
| --- | --- |
| 1.7.2 | 0.23 |
| 1.7.4 | 0.23 |
| 1.8.5 | 0.24 |

Latest Yaml version is: https://github.com/microsoft/PowerApps-Language-Tooling/blob/0a4c9e624ffad1f93b6c085aff029945a0fbc00a/src/PAModel/Serializers/SourceSerializer.cs#L49



## Test app usage

You can also use this functionality stand alone, using our test console app.

Download and install the [.NET Core SDK v6.0.x (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) in order to build.
Build the test console app by running: `\build.cmd`
This will create: `\bin\Debug\PASopa\PASopa.exe`

To unpack a .msapp file: `pasopa -unpack FromApp.msapp ToSourceFolder`
To pack a .msapp file: `pasopa -pack ToApp.msapp FromSourceFolder`

## Folder structure
Unpack and pack use this folder structure:

- **\src** - the control and component files. This contains the sources.
   - \*.fx.yaml - the formulas extracted from the control.json file.  **This is the place to edit your formulas.**-
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
The .fx.yaml files use a subset of [YAML](https://yaml.org/spec/1.2/spec.html).  Most notably and similar to Excel, all expressions must begin with an `=` sign.  More details are available [here](/docs/YAMLFileFormat.md)

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

## Known Issues
For a list of commonly known issues around pack and unpack and instructions for workarounds to resolve them please head over [here](/docs/KnownIssues.md)
