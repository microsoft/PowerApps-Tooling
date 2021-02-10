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
        public Dictionary<string, string> ClientPerFileChecksums { get; set; }

        // Checksum produced by server.
        public string ServerStampedChecksum { get; set; }

        public Dictionary<string, string> ServerPerFileChecksums { get; set; }

        public BuildVerJson ClientBuildDetails { get; set; }
    }
}
