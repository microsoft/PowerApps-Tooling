// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Functions
{
    public interface IFunction<T>
    {
        public string Name { get; }
        public bool TryDo(T thing, IEnumerable<string> args, out string result, out IEnumerable<IError> errors);
    }
}
