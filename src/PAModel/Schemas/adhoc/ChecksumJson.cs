// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal class ChecksumJson
    {
        // Checksum from client
        public string ClientStampedChecksum { get; set; }

        // Checksum produced by server.
        public string ServerStampedChecksum { get; set; }
    }
}
