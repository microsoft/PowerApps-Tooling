// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

public static partial class PaArchiveExtensions
{
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

    /// <summary>
    /// Uses <see cref="SHA256"/> to compute a hash of the contents of the entry.
    /// </summary>
    /// <remarks>
    /// The use of SHA-256 algorithm aligns with current Git hashing algorithm (see: https://git-scm.com/docs/hash-function-transition) along with modern security practices.
    /// </remarks>
    /// <returns>The hash as a string.</returns>
    public static string ComputeHash(this PaArchiveEntry entry)
    {
        using var stream = entry.Open();
        var hashBytes = SHA256.HashData(stream);
        return BitConverter.ToString(hashBytes);
    }

    public static string[] ReadAllLines(this PaArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), leaveOpen: false);
        var lines = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
            lines.Add(line);
        return [.. lines];
    }

    public static string ReadAllText(this PaArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), leaveOpen: false);
        return reader.ReadToEnd();
    }
}
