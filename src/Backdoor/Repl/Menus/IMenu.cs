// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Menus
{
    public interface IMenu<T>
    {
        public (IMenu<T>, string) TransferFunction(string input, T document, out IEnumerable<IError> errors);
        public string Title { get; }
        public string Description { get; }
    }
}
