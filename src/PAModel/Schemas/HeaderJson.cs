// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.AppMagic.Authoring.Persistence
{
    internal class HeaderJson
    {
        public Version DocVersion { get; set; }
        public Version MinVersionToLoad { get; set; }
        public Version MSAppStructureVersion { get; set; }
        public DateTime? LastSavedDateTimeUTC { get; set; }
    }
}
