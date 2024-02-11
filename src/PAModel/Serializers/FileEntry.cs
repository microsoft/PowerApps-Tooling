// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools.Extensions;
using Microsoft.PowerPlatform.Formulas.Tools.IO;


namespace Microsoft.PowerPlatform.Formulas.Tools;

// Raw kinds of files we recognize in the .msapp 
public enum FileKind
{
    Unknown,

    Properties,
    Header,
    AppCheckerResult,
    Checksum,

    // Used for dataComponents
    ComponentsMetadata,
    DataComponentSources,
    DataComponentTemplates,


    // If this file is present, it's an older format. 
    OldEntityJSon,

    // Resources 
    PublishInfo,

    // References 
    DataSources,
    Themes,
    Templates,
    Resources,
    Asset,

    // Category so 
    ControlSrc,
    ComponentSrc,
    TestSrc,

    // Unique to source format. 
    Entropy,
    CanvasManifest,
    Connections,
    ComponentReferences, // ComponentReferences.json

    // AppInsights
    AppInsightsKey,

    // AppTest parent control source file
    AppTestParentControl,

    // Schema.yaml describing app's parameters at top level. 
    Defines,

    // Custom page inputs for outbound custom page navigate calls.
    CustomPageInputs,
}

// Represent a file from disk or a Zip archive. 
[DebuggerDisplay("{Name}")]
internal class FileEntry
{
    // Name relative to root. Can be triaged to a FileKind
    public FilePath Name;

    public byte[] RawBytes;

    public FileEntry() { }

    public FileEntry(FileEntry other)
    {
        Name = other.Name;
        RawBytes = other.RawBytes.ToArray(); // ToArray clones byte arrays
    }

    public const string CustomPagesMetadataFileName = "CustomPagesMetadata.json";

    public static FileEntry FromFile(string fullPath, string root)
    {
        var relativePath = FilePath.GetRelativePath(root, fullPath);
        var bytes = File.ReadAllBytes(fullPath);
        var entry = new FileEntry
        {
            Name = FilePath.FromPlatformPath(relativePath),
            RawBytes = bytes
        };
        return entry;
    }

    public static FileEntry FromZip(ZipArchiveEntry z, string name = null)
    {
        if (name == null)
        {
            name = z.FullName;
            // Some paths mistakenly start with DirectorySepChar in the msapp,
            // We add _ to it when writing so that windows can handle it correctly. 
            if (z.FullName.StartsWith(Path.DirectorySeparatorChar.ToString()))
                name = FilenameLeadingUnderscore + z.FullName;
        }
        return new FileEntry
        {
            Name = FilePath.FromMsAppPath(name),
            RawBytes = z.ToBytes()
        };
    }

    public const char FilenameLeadingUnderscore = '_';

    // Map from path in .msapp to type. 
    internal static Dictionary<string, FileKind> _fileKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        {"Entities.json", FileKind.OldEntityJSon },
        {"Properties.json", FileKind.Properties },
        {"Header.json", FileKind.Header},
        {"Defines.fx.yaml", FileKind.Defines },
        {CustomPagesMetadataFileName, FileKind.CustomPageInputs },
        {ChecksumMaker.ChecksumName, FileKind.Checksum },
        {"AppCheckerResult.sarif", FileKind.AppCheckerResult },
        {"ComponentsMetadata.json", FileKind.ComponentsMetadata },
        {@"Resources\PublishInfo.json", FileKind.PublishInfo },
        {@"References\DataComponentSources.json", FileKind.DataComponentSources },
        {@"References\DataComponentTemplates.json", FileKind.DataComponentTemplates },
        {@"References\DataSources.json", FileKind.DataSources },
        {@"References\Themes.json", FileKind.Themes },
        {@"References\Templates.json", FileKind.Templates },
        {@"References\Resources.json", FileKind.Resources },

        // Files that only appear in Source
        {"Entropy.json", FileKind.Entropy },
        {"CanvasManifest.json", FileKind.CanvasManifest },
        {"ControlTemplates.json", FileKind.Templates },
        {"Connections.json", FileKind.Connections },
        {"ComponentReferences.json", FileKind.ComponentReferences },
        {"AppInsightsKey.json", FileKind.AppInsightsKey },
        { "Test_7F478737223C4B69.fx.yaml", FileKind.AppTestParentControl }
    };


    internal static FilePath GetFilenameForKind(FileKind kind)
    {
        var filename =
            (from kv in _fileKinds
             where kv.Value == kind
             select kv.Key).FirstOrDefault();

        return FilePath.FromMsAppPath(filename);
    }

    internal static FileKind TriageKind(FilePath fullname)
    {
        if (_fileKinds.TryGetValue(fullname.ToMsAppPath(), out var kind))
        {
            return kind;
        }

        // Source? 
        if (fullname.StartsWith(@"Controls", StringComparison.OrdinalIgnoreCase))
        {
            return FileKind.ControlSrc;
        }
        if (fullname.StartsWith(@"Components", StringComparison.OrdinalIgnoreCase))
        {
            return FileKind.ComponentSrc;
        }

        if (fullname.StartsWith(@"AppTests", StringComparison.OrdinalIgnoreCase))
        {
            return FileKind.TestSrc;
        }

        // Resource 
        if (fullname.StartsWith(@"Assets", StringComparison.OrdinalIgnoreCase))
        {
            return FileKind.Asset;
        }

        return FileKind.Unknown;
    }
}
