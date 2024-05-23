// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System.Linq;
using System.Diagnostics.Contracts;
using Microsoft.PowerPlatform.Formulas.Tools.IO;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class AddResource : IDelta
{
    private readonly FileEntry _file;
    private readonly ResourceJson _resource;
    private readonly string _name;

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

        document._resourcesJson.Resources = [.. resources.Values];
    }
}

internal class RemoveResource : IDelta
{
    private readonly string _name;
    private readonly FilePath _assetKey;

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
        document._resourcesJson.Resources = [.. resources.Values];

        if (_assetKey != null)
            document._assetFiles.Remove(_assetKey);
    }
}

internal class UpdateResource : IDelta
{
    private readonly string _name;
    private readonly FileEntry _file; // null if no contents.
    private readonly ResourceJson _resource;
    private readonly FilePath _assetKey; // needed for deletion (in case file is missing)

    public UpdateResource(FilePath assetKey, ResourceJson resource, FileEntry file)
    {
        _name = resource.Name;
        _file = file;
        _assetKey = assetKey;
        _resource = resource;

        Contract.Assert(assetKey != null);
        Contract.Assert(file == null || file.Name.Equals(assetKey));
    }

    public void Apply(CanvasDocument document)
    {
        var resources = document._resourcesJson.Resources.ToDictionary(res => res.Name);
        resources[_name] = _resource;
        document._resourcesJson.Resources = [.. resources.Values];

        document._assetFiles.Remove(_assetKey);

        if (_file != null)
            document.AddAssetFile(_file);
    }
}
