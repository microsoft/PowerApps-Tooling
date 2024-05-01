// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Writer which compares the output with the input during serialization.
/// </summary>
public class RoundTripWriter : TextWriter
{
    private const char NewLineChar = '\n';
    private readonly TextReader _input;
    private readonly string _entryFullPath;
    private int _lineNumber = 1;
    private int _columnNumber;
    private bool _exThrown;

    public RoundTripWriter(ZipArchiveEntry entry)
        : this(new StreamReader(entry.Open()), entry.FullName)
    {
    }

    public RoundTripWriter(TextReader input, string entryFullPath)
    {
        _input = input;
        _entryFullPath = entryFullPath;
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
        while ((inputValue = _input.Read()) == '\r') ;

        if (inputValue == -1 || inputValue != value)
        {
            _exThrown = true;
            throw new PaDiagnosticsException(new PaDiagnostic(PersistenceErrorCode.RoundTripValidationFailed, "Round trip serialization failed")
            {
                Origin = new(_entryFullPath)
                {
                    Start = (_lineNumber, _columnNumber),
                },
            });
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
            var inputValue = _input.Read();
            if (inputValue != -1)
            {
                throw new PaDiagnosticsException(new PaDiagnostic(PersistenceErrorCode.RoundTripValidationFailed, "Round trip serialization failed. Additional input not read when disposing")
                {
                    Origin = new(_entryFullPath)
                    {
                        Start = (_lineNumber, _columnNumber),
                    },
                });
            }
        }
        _input.Dispose();
        base.Dispose(disposing);
    }
}
