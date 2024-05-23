// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Writer which compares the output with the input during serialization.
/// </summary>
public class RoundTripWriter(TextReader input, string entryFullPath) : TextWriter
{
    private const char NewLineChar = '\n';
    private int _lineNumber = 1;
    private int _columnNumber;
    private bool _exThrown;

    public RoundTripWriter(ZipArchiveEntry entry)
#pragma warning disable CA2000 // This member is not used out of this scope
        : this(new StreamReader(entry.Open()), entry.FullName)
#pragma warning restore CA2200 // Rethrow to preserve stack details
    {
    }

    public override Encoding Encoding => Encoding.Default;

    public override void Write(char value)
    {
        _columnNumber++;
        if (value == '\r')
        {
            base.Write(value);
            return;
        }

        // Read each char while skipping the \r
        int inputValue;
        while ((inputValue = input.Read()) == '\r') ;

        if (inputValue == -1 || inputValue != value)
        {
            _exThrown = true;
            throw new PersistenceLibraryException(PersistenceErrorCode.RoundTripValidationFailed, $"Round trip serialization failed")
            {
                MsappEntryFullPath = entryFullPath,
                LineNumber = _lineNumber,
                Column = _columnNumber
            };
        }

        if (value == NewLineChar)
        {
            _lineNumber++;
            _columnNumber = 0;
        }

        base.Write(value);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_exThrown) // Don't throw a new exception in finally block if an exception was already thrown
        {
            // We need to make sure that we have read all the input.
            var inputValue = input.Read();
            if (inputValue != -1)
            {
                throw new PersistenceLibraryException(PersistenceErrorCode.RoundTripValidationFailed, $"Round trip serialization failed. Additional input not read when disposing.")
                {
                    MsappEntryFullPath = entryFullPath,
                    LineNumber = _lineNumber,
                    Column = _columnNumber
                };
            }
        }
        input.Dispose();
        base.Dispose(disposing);
    }
}
