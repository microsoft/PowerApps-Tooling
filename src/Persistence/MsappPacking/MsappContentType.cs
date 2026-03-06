// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// The content type of some msapp entry for the purposes of unpacking.
/// </summary>
public enum MsappContentType
{
    /// <summary>
    /// Indicates the content is not significant or applicable to an an unpacked app's layout.
    /// This content should be maintained unmodified, but are not significant to tooling.
    /// </summary>
    Other = 0,

    Header = 1,

    /// <summary>
    /// The contents of the 'Src' folder in the msapp, which includes the *.pa.yaml files.
    /// </summary>
    PaYamlSourceCode,

    /// <summary>
    /// The contents of the 'Assets' folder. Often unpacked in order to reuce file size of the .msapr file.
    /// </summary>
    Asset,
}
