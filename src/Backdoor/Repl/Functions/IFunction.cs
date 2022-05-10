// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace Backdoor.Repl.Functions
{
    public interface IFunction<T>
    {
        public string Name { get; }
        public IResult<ICanvasDocument> Invoke(T thing, IEnumerable<string> args);
    }
}
