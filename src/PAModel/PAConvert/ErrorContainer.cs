// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// TODO: Sort Imports
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    /// <summary>
    /// Container for errors (which may have prevented the operation)
    /// or warnings (which are informational, but could be ignored).
    /// </summary>
    public class ErrorContainer : IEnumerable<Error>
    {
        private List<Error> _errors = new List<Error>();
        private int _countErrors = 0;
        private int _countWarnings = 0;
        // TODO: Determine limit
        private const int _errorsToPrint = 10; // Enforcing a limit in code to prevent oversized message body from getting trimmed

        internal void AddError(ErrorCode code, SourceLocation span, string errorMessage)
        {
            var error = new Error(code, span, errorMessage);
            // TODO: Capture count in a new private variable
            if (error.IsError) { _countErrors++; } else { _countWarnings++; }

            if (_errors.Count < _errorsToPrint)
            {
                _errors.Add(error);
            }
        }

        public bool HasErrors => _errors.Any(error => error.IsError);

        public bool HasWarnings => _errors.Any(error => error.IsWarning);

        // Helper for interupting processing once we have errors.
        // Ignores warnings. 
        internal void ThrowOnErrors()
        {
            if (this.HasErrors)
            {
                throw new DocumentException();
            }
        }

        public IEnumerator<Error> GetEnumerator()
        {
            return this._errors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable inner = this._errors;
            return inner.GetEnumerator();
        }

        // Helper for writing out errors.
        public void Write(TextWriter output)
        {
            foreach (var error in this)
            {
                // if (error.IsError) { countErrors++; } else { countWarnings++; }
                output.WriteLine(error);
            }

            if (_countErrors > _errorsToPrint)
            {
                var additionalErrors = _countErrors - _errorsToPrint;
                output.WriteLine($"...and {additionalErrors} errors.");
            }

            if (_countErrors + _countWarnings > 0)
            {
                output.WriteLine($"{_countErrors} errors, {_countWarnings} warnings.");
            }
        }

        public override string ToString()
        {
            StringWriter s = new StringWriter();
            Write(s);
            return s.ToString();
        }
    }
}
