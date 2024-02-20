// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Writer which compares the output with the input during serialization.
/// </summary>
internal class RoundTripWriter : TextWriter
{
    private const char NewLineChar = '\n';
    private readonly TextReader _input;
    private readonly string _inputFileName;
    private int _lineNumber = 1;
    private int _columnNumber;

    public RoundTripWriter(TextReader input, string inputFileName)
    {
        _input = input;
        _inputFileName = inputFileName;
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
            throw new PersistenceException($"Round trip serialization failed")
            {
                FileName = _inputFileName,
                Line = _lineNumber,
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
        // We need to make sure that we have read all the input.
        var inputValue = _input.Read();
        if (inputValue != -1)
        {
            throw new PersistenceException($"Round trip serialization failed")
            {
                FileName = _inputFileName,
                Line = _lineNumber,
                Column = _columnNumber
            };
        }
        _input.Dispose();
        base.Dispose(disposing);
    }
}
