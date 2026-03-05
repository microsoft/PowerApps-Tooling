// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

public static class PaArchiveEntryExtensions
{
    public static void ExtractToFile(this PaArchiveEntry source, string destinationFileName, bool overwrite = false)
    {
        source.ZipEntry.ExtractToFile(destinationFileName, overwrite);
    }

    public static T DeserializeAsJson<T>(this PaArchiveEntry entry, JsonSerializerOptions serializerOptions)
        where T : notnull
    {
        // TODO: Create a new exception 'PaArchiveException' set of exceptions to target archive-only type errors.
        // For now though, calling code currently expects PersistenceLibraryException so we'll keep it.
        // maybe 'PaArchiveException' can inherit from PersistenceLibraryException, and we can use it for more specific error codes and to avoid confusion with other types of PersistenceLibraryExceptions that are not archive related.
        try
        {
            return JsonSerializer.Deserialize<T>(entry.Open(), serializerOptions)
                 ?? throw new PersistenceLibraryException(PersistenceErrorCode.PaArchiveEntryDeserializedToJsonNull, "Deserialization of json file resulted in null object.")
                 {
                     MsappEntryFullPath = entry.FullName,
                 };
        }
        catch (JsonException ex)
        {
            throw new PersistenceLibraryException(PersistenceErrorCode.PaArchiveEntryDeserializedToJsonFailed, $"Failed to deserialize json file to an instance of {typeof(T).Name}.", ex)
            {
                MsappEntryFullPath = entry.FullName,
                LineNumber = ex.LineNumber,
                Column = ex.BytePositionInLine,
                JsonPath = ex.Path,
            };
        }
    }
}
