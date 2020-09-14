// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    class ChecksumJson
    {
        // Checksum from client
        public string Checksum { get; set; }

        // Checksum produced by server.
        public string ChecksumServer { get; set; }
    }
}
