// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Serialization;

/// <summary>
/// Shared constants used for .msapr serialization and deserialization.
/// </summary>
internal static class MsaprSerialization
{
    public static readonly JsonSerializerOptions DefaultJsonSerializeOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,

        Converters =
        {
            // TODO: ensure we save date-times in UTC round-tripable format
        },

        // Deserialization only options:
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        // In order to ensure forward-compatible deserialization, we ignore unknown members
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };
}
