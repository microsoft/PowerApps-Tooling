// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // ResouceJson.cs file has an entry for all the resources that are referred in the app.
    // The resources are of two kinds: Uri and LocalFile
    // The information for the LocalFile resources can be emitted since this information can be dynamically generated, based on the files present in Assets directory.
    internal static class TranformResourceJson
    {
        public static Regex ImageExtensionRegEx = new Regex(".*\\.(?i)(gif|jpg|png|bmp|jpeg|tiff|tif|svg)$", RegexOptions.IgnoreCase);

        // Media Extension is a union of audio and video extensions we support.
        public static Regex MediaExtensionRegEx = new Regex(".*\\.(?i)(mp3|wav|wma|mp4|wmv)$", RegexOptions.IgnoreCase);

        public static Regex AudioExtensionRegEx = new Regex(".*\\.(?i)(mp3|wav|wma)$", RegexOptions.IgnoreCase);

        public static Regex VideoExtensionRegEx = new Regex(".*\\.(?i)(mp4|wmv)$", RegexOptions.IgnoreCase);

        public static Regex PdfExtensionRegEx = new Regex(".*\\.(?i)(pdf)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Remove the LocalFile entries from Resources.json and also persist the index of each entry in entropy.
        /// </summary>
        /// <param name="app"></param>
        public static void TranformResourceJsonOnLoad(this CanvasDocument app)
        {
            for (var i = 0; i < app._resourcesJson.Resources.Length; i++)
            {
                if (app._entropy?.ResourceJsonIndexes != null && !app._entropy.ResourceJsonIndexes.ContainsKey(app._resourcesJson.Resources[i].Name))
                {
                    app._entropy.ResourceJsonIndexes.Add(app._resourcesJson.Resources[i].Name, i);
                }
            }
            app._resourcesJson.Resources = app._resourcesJson.Resources.Where(x => x.ResourceKind != ResourceKind.LocalFile).ToArray();
        }

        /// <summary>
        /// Adds the entries of LocalFile assets back to Resources.json in an ordered manner.
        /// </summary>
        /// <param name="app"></param>
        public static void TransformResourceJsonOnSave(this CanvasDocument app)
        {
            var localFileResourceJsonEntries = new List<ResourceJson>();
            var logoFileName = app._logoFile?.Name?.GetFileName();

            // Iterate through local asset files to add their entries back to Resources.Json
            foreach (var file in app._assetFiles)
            {
                var fileName = file.Key.GetFileName();
                if (!app._resourcesJson.Resources.Any(x => x.FileName == fileName) && logoFileName != fileName)
                {
                    localFileResourceJsonEntries.Add(GenerateResourceJsonEntryFromAssetFile(file.Key));
                }
            }
            app._resourcesJson.Resources = app._resourcesJson.Resources.Concat(localFileResourceJsonEntries).ToArray();

            // bring the order of resourceJson back to avoid checksum violation.
            if (app._entropy?.ResourceJsonIndexes != null && app._entropy.ResourceJsonIndexes.Count > 0)
            {
                var orderedResourcesList = new List<ResourceJson>();
                var orderedIndices = app._entropy.ResourceJsonIndexes.OrderBy(x => x.Value);
                foreach (var kvp in orderedIndices)
                {
                    var resource = app._resourcesJson.Resources.Where(x => x.Name == kvp.Key);
                    orderedResourcesList.Add(resource.SingleOrDefault());
                }

                // handle the cases when some new files were added to the asset folder offline. The entries for the new assets would go at the end, after all the ordered resources have been added.
                orderedResourcesList.AddRange(app._resourcesJson.Resources.Where(x => !app._entropy.ResourceJsonIndexes.ContainsKey(x.Name)));
                app._resourcesJson.Resources = orderedResourcesList.ToArray();
            }
        }

        // create the resource json entry dynamically from the asset file.
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
    }

}
