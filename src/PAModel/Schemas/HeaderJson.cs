//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
