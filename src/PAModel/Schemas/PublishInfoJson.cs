// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.AppMagic.Authoring.Persistence
{
    internal class PublishInfoJson
    {
        public string AppName { get; set; }
        public string BackgroundColor { get; set; }
        public string PublishTarget { get; set; }

        // This is always a msapp-style path (windows style)
        public string LogoFileName { get; set; }
        public bool PublishResourcesLocally { get; set; }
        public bool PublishDataLocally { get; set; }
        public string UserLocale { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
