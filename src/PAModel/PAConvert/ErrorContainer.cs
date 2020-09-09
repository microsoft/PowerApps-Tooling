// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PAModel.PAConvert.Parser;
using System.Collections.Generic;
using System.Linq;

namespace PAModel.PAConvert
{
    public class ErrorContainer
    {
        private List<PAError> _errors;

        public ErrorContainer()
        {
            _errors = new List<PAError>();
        }

        internal void AddError(TokenSpan span, string errorMessage)
        {
            _errors.Add(new PAError(span, errorMessage));
        }

        public bool HasErrors()
        {
            return _errors.Any();
        }

        public IEnumerable<PAError> Errors()
        {
            return _errors.AsEnumerable();
        }
    }
}
