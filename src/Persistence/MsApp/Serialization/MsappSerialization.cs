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
public static class MsappSerialization
{
    /// <summary>
    /// This should match the options used in DocumentServer for deserializing msapp json files.
    /// See: JsonDocumentSerializer.SerializerOptions in DocumentServer.Core.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultSharedJsonSerializeOptions = new JsonSerializerOptions()
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
    }.ToReadOnly();

    internal static readonly JsonSerializerOptions DocumentJsonSerializeOptions = new JsonSerializerOptions(DefaultSharedJsonSerializeOptions)
        .ToReadOnly();

    /// <summary>
    /// This should match the options used in DocumentServer for deserializing msapp json files.
    /// See: JsonDocumentSerializer.SerializerOptions in DocumentServer.Core.
    /// </summary>
    public readonly static JsonSerializerOptions HeaderJsonSerializeOptions = new JsonSerializerOptions(DefaultSharedJsonSerializeOptions)
    {
        // Note: The docsvr doesn't indent the Header.json file.
        WriteIndented = false,
    }.ToReadOnly();

    /// <summary>
    /// Serialization options used for the 'packed.json' file in the msapp archive.
    /// </summary>
    public static readonly JsonSerializerOptions PackedJsonSerializeOptions = new JsonSerializerOptions()
    {
        // Note: We explicitly don't derive from the default, since this is a net-new file which is fully owned by this library.
        Converters =
        {
            new JsonDateTimeUtcConverter(),
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
