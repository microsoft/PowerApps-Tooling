// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System.Collections;
using System.IO;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools;

/// <summary>
/// Container for errors (which may have prevented the operation)
/// or warnings (which are informational, but could be ignored).
/// </summary>
public class ErrorContainer : IEnumerable<Error>
{
    private readonly List<Error> _errors = new();

    internal void AddError(ErrorCode code, SourceLocation span, string errorMessage)
    {
        _errors.Add(new Error(code, span, errorMessage));
    }

    public bool HasErrors => _errors.Any(error => error.IsError);

    public bool HasWarnings => _errors.Any(error => error.IsWarning);

    // Helper for interupting processing once we have errors.
    // Ignores warnings. 
    internal void ThrowOnErrors()
    {
        if (HasErrors)
        {
            throw new DocumentException();
        }
    }

    public IEnumerator<Error> GetEnumerator()
    {
        return _errors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        IEnumerable inner = _errors;
        return inner.GetEnumerator();
    }

    // Helper for writing out errors.
    public void Write(TextWriter output)
    {
        var countWarnings = 0;
        var countErrors = 0;

        foreach (var error in this)
        {
            if (error.IsError) { countErrors++; } else { countWarnings++; }
            output.WriteLine(error);
        }

        if (countErrors + countWarnings > 0)
        {
            output.WriteLine($"{countErrors} errors, {countWarnings} warnings.");
        }
    }

    public override string ToString()
    {
        var s = new StringWriter();
        Write(s);
        return s.ToString();
    }
}
