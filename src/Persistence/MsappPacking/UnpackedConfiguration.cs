// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Shared configuration of an unpacked msapp which can be referenced by tooling to ensure consistent behaviors.
/// </summary>
public sealed record UnpackedConfiguration
{
    private static readonly ImmutableArray<MsappUnpackableContentType> DeaultContentTypes = [MsappUnpackableContentType.PaYamlSourceCode];

    /// <summary>
    /// The types of content in the msapp which should be unpacked or were previously unpacked.
    /// </summary>
    public ImmutableArray<MsappUnpackableContentType> ContentTypes { get; init; } = DeaultContentTypes;

    public bool EnablesContentType(MsappUnpackableContentType contentType)
    {
        return ContentTypes.Contains(contentType);
    }
}
