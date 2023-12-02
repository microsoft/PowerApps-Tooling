// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.PowerPlatform.Formulas.Tools;

// ResourceJson.cs file has an entry for all the resources that are referred in the app.
// The resources are of two kinds: Uri and LocalFile
// The information for the LocalFile resources can be emitted since this information can be dynamically generated, based on the files present in Assets directory.
internal static class TransformResourceJson
{
    public static Regex ImageExtensionRegEx = new Regex(".*\\.(?i)(gif|jpg|png|bmp|jpeg|tiff|tif|svg)$", RegexOptions.IgnoreCase);

    // Media Extension is a union of audio and video extensions we support.
    public static Regex MediaExtensionRegEx = new Regex(".*\\.(?i)(mp3|wav|wma|mp4|wmv)$", RegexOptions.IgnoreCase);

    public static Regex AudioExtensionRegEx = new Regex(".*\\.(?i)(mp3|wav|wma)$", RegexOptions.IgnoreCase);

    public static Regex VideoExtensionRegEx = new Regex(".*\\.(?i)(mp4|wmv)$", RegexOptions.IgnoreCase);

    public static Regex PdfExtensionRegEx = new Regex(".*\\.(?i)(pdf)$", RegexOptions.IgnoreCase);

    /// <summary>
    /// Persists the original order of resources in Resources.json in Entropy.
    /// </summary>
    /// <param name="app">The app.</param>
    public static void PersistOrderingOfResourcesJsonEntries(this CanvasDocument app)
    {
        for (var i = 0; i < app._resourcesJson.Resources.Length; i++)
        {
            app._entropy.Add(app._resourcesJson.Resources[i], i);
        }
    }

    /// <summary>
    /// Adds the entries of LocalFile assets to the Resources.json in an ordered manner.
    /// </summary>
    /// <param name="app">The app.</param>
    public static void AddLocalAssetEntriesToResourceJson(this CanvasDocument app)
    {
        var localFileResourceJsonEntries = new List<ResourceJson>();

        // Iterate through local asset files to add their entries back to Resources.Json
        foreach (var file in app._assetFiles)
        {
            var fileName = file.Key.GetFileName();
            if (!app._resourcesJson.Resources.Any(x => x.FileName == fileName) && !IsLogoFile(file.Key, app))
            {
                localFileResourceJsonEntries.Add(GenerateResourceJsonEntryFromAssetFile(file.Key));
            }
        }
        app._resourcesJson.Resources = app._resourcesJson.Resources.Concat(localFileResourceJsonEntries).ToArray();

        // Bring the order of resourceJson back to avoid checksum violation.
        if (app._entropy?.ResourcesJsonIndices != null && app._entropy.ResourcesJsonIndices.Count > 0)
        {
            var orderedResourcesList = new List<ResourceJson>();
            var orderedIndices = app._entropy.ResourcesJsonIndices.OrderBy(x => x.Value);
            foreach (var kvp in orderedIndices)
            {
                var resourceName = app._entropy.GetResourceNameFromKey(kvp.Key);
                var resource = app._resourcesJson.Resources.Where(x => x.Name == resourceName);
                orderedResourcesList.Add(resource.SingleOrDefault());
            }

            // Handle the cases when some new files were added to the asset folder offline. The entries for the new assets would go at the end, after all the ordered resources have been added.
            orderedResourcesList.AddRange(app._resourcesJson.Resources.Where(x => !app._entropy.ResourcesJsonIndices.ContainsKey(app._entropy.GetResourcesJsonIndicesKey(x))));
            app._resourcesJson.Resources = orderedResourcesList.ToArray();
        }
    }

    // Create the resource json entry dynamically from the asset file.
    private static ResourceJson GenerateResourceJsonEntryFromAssetFile(FilePath filePath)
    {
        var fileName = filePath.GetFileName();
        var contentKind = GetContentKind(fileName);
        return new ResourceJson()
        {
            Name = filePath.GetFileNameWithoutExtension(),
            Content = GetContentKind(fileName),
            Schema = GetSchema(contentKind),
            ResourceKind = ResourceKind.LocalFile,
            Path = FilePath.RootedAt("Assets", filePath).ToMsAppPath(),
            FileName = fileName,
            Type = "ResourceInfo",
            IsSampleData = false,
            IsWritable = false
        };
    }

    // Get the ContentKind of the resource based on its file extension.
    private static ContentKind GetContentKind(string name)
    {
        if (ImageExtensionRegEx.IsMatch(name))
            return ContentKind.Image;
        else if (AudioExtensionRegEx.IsMatch(name))
            return ContentKind.Audio;
        else if (VideoExtensionRegEx.IsMatch(name))
            return ContentKind.Video;
        else if (PdfExtensionRegEx.IsMatch(name))
            return ContentKind.Pdf;
        else
            return ContentKind.Unknown;
    }

    // Get the Schema type of the resource based on its ContentKind.
    private static string GetSchema(ContentKind contentKind)
    {
        string schema;
        switch (contentKind)
        {
            case ContentKind.Image:
                schema = Schema.i.ToString();
                break;
            case ContentKind.Audio:
            case ContentKind.Video:
                schema = Schema.m.ToString(); ;
                break;
            case ContentKind.Pdf:
                schema = Schema.o.ToString(); ;
                break;
            default:
                Contract.Assert(contentKind == ContentKind.Unknown);
                schema = "?";
                break;
        }

        return schema;
    }

    private static bool IsLogoFile(FilePath path, CanvasDocument app)
    {
        var logoFileName = app._logoFile?.Name?.GetFileName();
        return string.IsNullOrEmpty(logoFileName) ? false : path.Equals(new FilePath(logoFileName));
    }
}
