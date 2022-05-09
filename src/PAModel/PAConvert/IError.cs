// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.PAConvert
{
    public interface IError
    {
        public bool IsError { get; }
        public bool IsWarning { get; }
    }
}
