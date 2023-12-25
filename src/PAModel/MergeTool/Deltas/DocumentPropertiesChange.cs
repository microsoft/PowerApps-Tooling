// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class DocumentPropertiesChange : IDelta
{
    public readonly string Name;
    private readonly object _propertyValue;
    private readonly JsonElement _extensionDataValue;
    private readonly bool _isExtensionData;
    private readonly bool _wasRemoved;

    public DocumentPropertiesChange(string name, object value)
    {
        Name = name;
        _propertyValue = value;
        _isExtensionData = false;
    }

    public DocumentPropertiesChange(string name, JsonElement value)
    {
        Name = name;
        _extensionDataValue = value;
        _isExtensionData = true;
    }

    public DocumentPropertiesChange(string name, bool wasRemoved)
    {
        Name = name;
        _isExtensionData = true;
        _wasRemoved = true;
    }


    public void Apply(CanvasDocument document)
    {
        if (_isExtensionData)
        {
            if (_wasRemoved)
                document._properties.ExtensionData.Remove(Name);
            else
                document._properties.ExtensionData[Name] = _extensionDataValue;

            return;
        }

        var field = typeof(DocumentPropertiesJson).GetProperty(Name);
        field.SetValue(document._properties, _propertyValue);
    }
}
