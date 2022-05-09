// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor
{
    public class BackdoorError : IError
    {
        private string _message;

        public BackdoorError(bool isError, bool isWarning, string message)
        {
            IsError = isError;
            IsWarning = isWarning;
            _message = message;
        }

        public bool IsError { get; }
        public bool IsWarning { get; }
        public override string ToString() => _message;
    }
}
