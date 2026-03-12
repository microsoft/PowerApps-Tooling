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
public static class MsaprSerialization
{
    public static readonly JsonSerializerOptions DefaultJsonSerializeOptions = new JsonSerializerOptions()
    {
        Converters =
        {
            // If we ever need to save DateTime values, we should do so using the following converter to ensure correct serialization as UTC time:
            //new JsonDateTimeUtcConverter(),
        },

        // Use WhenWritingNull so that non-nullable value types (e.g. bool) are always written,
        // which is required for round-tripping 'required' properties whose value equals the type default (e.g. LoadFromYaml=false).
        // WhenWritingDefault would silently omit those properties, causing deserialization failures.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,

        // Deserialization only options:
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        // In order to ensure forward-compatible deserialization, we ignore unknown members
        // Any object model that wants to also survive round-tripping, must use JsonExtensionData to capture those unknown members.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    }.ToReadOnly();
}
