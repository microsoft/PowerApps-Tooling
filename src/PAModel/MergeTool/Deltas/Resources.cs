using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class AddResource : IDelta
    {
        public FileEntry File;
        public ResourceJson Resource;
        public string Name;

        public void Apply(CanvasDocument document)
        {
            var resources = document._resourcesJson.Resources.ToDictionary(res => res.Name);
            if (resources.ContainsKey(Name))
                return;

            resources.Add(Name, Resource);
            if (Resource.ResourceKind == "LocalFile")
            {
                document.AddAssetFile(File);
            }

            document._resourcesJson.Resources = resources.Values.ToArray();
        }
    }
    internal class RemoveResource : IDelta
    {
        public string Name;

        public void Apply(CanvasDocument document)
        {
            var resources = document._resourcesJson.Resources.ToDictionary(res => res.Name);
            resources.Remove(Name);
            document._resourcesJson.Resources = resources.Values.ToArray();
        }
    }
}
