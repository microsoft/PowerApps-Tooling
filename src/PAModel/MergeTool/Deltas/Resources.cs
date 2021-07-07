using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class AddResource : IDelta
    {
        private FileEntry _file;
        private ResourceJson _resource;
        private string _name;

        public AddResource(string name, ResourceJson resource, FileEntry file)
        {
            _file = file;
            _resource = resource;
            _name = name;
        }

        public AddResource(string name, ResourceJson resource)
        {
            _file = null;
            _resource = resource;
            _name = name;
        }

        public void Apply(CanvasDocument document)
        {
            var resources = document._resourcesJson.Resources.ToDictionary(res => res.Name);
            if (resources.ContainsKey(_name))
                return;

            resources.Add(_name, _resource);
            if (_resource.ResourceKind == ResourceKind.LocalFile && _file != null)
            {
                document.AddAssetFile(_file);
            }

            document._resourcesJson.Resources = resources.Values.ToArray();
        }
    }

    internal class RemoveResource : IDelta
    {
        private string _name;
        private FilePath _assetKey;

        public RemoveResource(string name, FilePath assetKey)
        {
            _name = name;
            _assetKey = assetKey;
        }

        public RemoveResource(string name)
        {
            _name = name;
            _assetKey = null;
        }

        public void Apply(CanvasDocument document)
        {
            var resources = document._resourcesJson.Resources.ToDictionary(res => res.Name);
            resources.Remove(_name);
            document._resourcesJson.Resources = resources.Values.ToArray();

            if (_assetKey != null)
                document._assetFiles.Remove(_assetKey);
        }
    }

    internal class UpdateResource : IDelta
    {
        private string _name;
        private FileEntry _file;
        private ResourceJson _resource;
        private FilePath _assetKey;

        public UpdateResource(string name, FilePath assetKey, ResourceJson resource, FileEntry file)
        {
            _name = name;
            _file = file; 
            _assetKey = assetKey;
            _resource = resource;
        }

        public UpdateResource(string name, ResourceJson resource)
        {
            _name = name;
            _file = null;
            _assetKey = null;
            _resource = resource;
        }

        public void Apply(CanvasDocument document)
        {
            var resources = document._resourcesJson.Resources.ToDictionary(res => res.Name);
            resources.Remove(_name);
            resources.Add(_name, _resource);
            document._resourcesJson.Resources = resources.Values.ToArray();

            if (_assetKey == null)
                document._assetFiles.Remove(_assetKey);

            if (_file != null)
                document.AddAssetFile(_file);
        }
    }
}
