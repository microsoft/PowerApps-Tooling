// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

public static partial class PaArchiveExtensions
{
    /// <summary>
    /// Reads the contents of the entry as JSON and deserializes it to an instance of type <typeparamref name="T"/> using <see cref="JsonSerializer"/>.
    /// </summary>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="PersistenceLibraryException">The json represents a value of null, or an exception occured during deserialization.</exception>
    public static T DeserializeAsJson<T>(this PaArchiveEntry entry, JsonSerializerOptions serializerOptions)
        where T : notnull
    {
        using var entryStream = entry.Open();

        try
        {
            return JsonSerializer.Deserialize<T>(entryStream, serializerOptions)
                ?? throw CreatePersistenceExceptionForNullJsonEntry(entry);
        }
        catch (JsonException ex)
        {
            throw CreatePersistenceExceptionFrom<T>(ex, entry);
        }
    }

    /// <summary>
    /// Asynchronously reads the contents of the entry as JSON and deserializes it to an instance of type <typeparamref name="T"/> using <see cref="JsonSerializer"/>.
    /// </summary>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="PersistenceLibraryException">The json represents a value of null, or an exception occured during deserialization.</exception>
    public static async Task<T> DeserializeAsJsonAsync<T>(this PaArchiveEntry entry, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken = default)
        where T : notnull
    {
        using var entryStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await JsonSerializer.DeserializeAsync<T>(entryStream, serializerOptions, cancellationToken).ConfigureAwait(false)
                ?? throw CreatePersistenceExceptionForNullJsonEntry(entry);
        }
        catch (JsonException ex)
        {
            throw CreatePersistenceExceptionFrom<T>(ex, entry);
        }
    }

    private static PersistenceLibraryException CreatePersistenceExceptionForNullJsonEntry(PaArchiveEntry entry)
    {
        return new(PersistenceErrorCode.PaArchiveEntryDeserializedToJsonNull, "Deserialization of json file resulted in null object.")
        {
            MsappEntryFullPath = entry.FullName,
        };
    }

    private static PersistenceLibraryException CreatePersistenceExceptionFrom<T>(JsonException ex, PaArchiveEntry entry) where T : notnull
    {
        // TODO: Create a new exception 'PaArchiveException' set of exceptions to target archive-only type errors.
        // For now though, calling code currently expects PersistenceLibraryException so we'll keep it.
        // maybe 'PaArchiveException' can inherit from PersistenceLibraryException, and we can use it for more specific error codes and to avoid confusion with other types of PersistenceLibraryExceptions that are not archive related.
        return new(PersistenceErrorCode.PaArchiveEntryDeserializedToJsonFailed, $"Failed to deserialize json file to an instance of {typeof(T).Name}.", ex)
        {
            MsappEntryFullPath = entry.FullName,
            LineNumber = ex.LineNumber,
            Column = ex.BytePositionInLine,
            JsonPath = ex.Path,
        };
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
#if NET5_0_OR_GREATER
        var hashBytes = SHA256.HashData(stream);
#else
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
#endif
        return BitConverter.ToString(hashBytes);
    }

    public static string[] ReadAllLines(this PaArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        var lines = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
            lines.Add(line);
        return [.. lines];
    }

    public static string ReadAllText(this PaArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
