// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas;

// This schema is only for the unpacked sources. It is not part of the .msapp file.
internal class LocalAssetInfoJson
{
    public string OriginalName { get; set; }
    public string NewFileName { get; set; }
    public string Path { get; set; }
}
