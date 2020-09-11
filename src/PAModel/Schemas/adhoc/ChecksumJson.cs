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
