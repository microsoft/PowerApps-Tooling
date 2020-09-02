using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;


namespace PAModel
{
    // Raw kinds of files we recognize in the .msapp 
    enum FileKind
    {
        Unknown,

        Properties,
        Header,
        AppCheckerResult,

        // Used for dataComponents
        ComponentsMetadata,
        DataComponentSources,
        DataComponentTemplates,


        // If this file is present, it's an older format. 
        OldEntityJSon,

        // Resourcs 
        PublishInfo,

        // References 
        DataSources,
        Themes,
        Templates,
        Resources,
        DynamicTypes,

        // Category so 
        ControlSrc,
        ComponentSrc,        

        // Unique to source format. 
        Entropy,
        CanvasManifest
    }

    // Represent a file from disk or a Zip archive. 
    [DebuggerDisplay("{Name}")]
    class FileEntry
    {
        // Name relative to root. Can be triaged to a FileKind
        public string Name;
        
        public byte[] RawBytes;

        public static FileEntry FromFile(string fullPath, string root)
        {
            var relativePath = Utility.GetRelativePath(fullPath, root);
            var bytes = File.ReadAllBytes(fullPath);
            var entry = new FileEntry
            {
                Name = relativePath.Replace('/', '\\'),
                RawBytes = bytes
            };
            return entry;
        }

        public static FileEntry FromZip(ZipArchiveEntry z)
        {
            return new FileEntry
            {
                Name = z.FullName,
                RawBytes = z.ToBytes()
            };
        }


        internal static Dictionary<string, FileKind> _fileKinds = new Dictionary<string, FileKind>(StringComparer.OrdinalIgnoreCase)
        {
            {"Entities.json", FileKind.OldEntityJSon },
            {"Properties.json", FileKind.Properties },
            {"Header.json", FileKind.Header},
            {"AppCheckerResult.sarif", FileKind.AppCheckerResult },
            {"ComponentsMetadata.json", FileKind.ComponentsMetadata },
            {@"Resources\PublishInfo.json", FileKind.PublishInfo },
            {@"References\DataComponentSources.json", FileKind.DataComponentSources },
            {@"References\DataComponentTemplates.json", FileKind.DataComponentTemplates },
            {@"References\DataSources.json", FileKind.DataSources },
            {@"References\Themes.json", FileKind.Themes },
            {@"References\Templates.json", FileKind.Templates },
            {@"References\Resources.json", FileKind.Resources },
            {@"References\DynamicTypes.json", FileKind.DynamicTypes },

            {"Entropy.json", FileKind.Entropy },
            {"CanvasManifest.json", FileKind.CanvasManifest }
            
        };


        internal static string GetFilenameForKind(FileKind kind)
        {
            string filename =
                (from kv in _fileKinds
                 where kv.Value == kind
                 select kv.Key).FirstOrDefault();

            return filename;
        }

        internal static FileKind TriageKind(string fullname)
        {
            FileKind kind;
            if (_fileKinds.TryGetValue(fullname, out kind))
            {
                return kind;
            }

            // Source? 
            if (fullname.StartsWith(@"Controls\", StringComparison.OrdinalIgnoreCase))
            {
                return FileKind.ControlSrc;
            }
            if (fullname.StartsWith(@"Components\", StringComparison.OrdinalIgnoreCase))
            {
                return FileKind.ComponentSrc;
            }
            return FileKind.Unknown;
        }
    }
}