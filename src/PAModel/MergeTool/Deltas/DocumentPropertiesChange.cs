// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class DocumentPropertiesChange : IDelta
    {
        public readonly string Name;
        private object _propertyValue;
        private JsonElement _extensionDataValue;
        private bool _isExtensionData;
        private bool _wasRemoved = false;

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
}
