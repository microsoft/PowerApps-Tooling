// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools;

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
