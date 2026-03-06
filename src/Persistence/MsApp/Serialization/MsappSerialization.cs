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

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;

/// <summary>
/// Shared constants used for .msapp serialization and deserialization.
/// </summary>
internal static class MsappSerialization
{
    /// <summary>
    /// This should match the options used in DocumentServer for deserializing msapp json files.
    /// See: JsonDocumentSerializer.SerializerOptions in DocumentServer.Core.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultSharedJsonSerializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true, // Most files are serialized with indentation
        // We don't want to fail if there are extra properties in the json
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        Converters =
        {
            // Note: this converter for the document has a bug when serializing that it doesn't
            // force DateTime values into UTC time.
            // But this may have impact on other code which depends on this property.
            new JsonDateTimeAssumesUtcConverter(),
        },
    };

    public static readonly JsonSerializerOptions DocumentJsonSerializeOptions = new(DefaultSharedJsonSerializeOptions);

    /// <summary>
    /// This should match the options used in DocumentServer for deserializing msapp json files.
    /// See: JsonDocumentSerializer.SerializerOptions in DocumentServer.Core.
    /// </summary>
    public readonly static JsonSerializerOptions HeaderJsonSerializeOptions = new(DefaultSharedJsonSerializeOptions)
    {
        WriteIndented = false,
    };

    /// <summary>
    /// Serialization options used for the 'packed.json' file in the msapp archive.
    /// </summary>
    public static readonly JsonSerializerOptions PackedJsonSerializeOptions = new()
    {
        // Note: We explicitly don't derive from the default, since this is a net-new file which is fully owned by this library.
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        Converters =
        {
            new JsonDateTimeUtcConverter(),
        },
    };
}
