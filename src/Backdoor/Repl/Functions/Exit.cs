// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Functions
{
    public class Exit : IFunction<ICanvasDocument>
    {
        public string Name => "exit";
        public bool TryDo(ICanvasDocument thing, IEnumerable<string> args, out string result, out IEnumerable<IError> errors)
        {
            Environment.Exit(0);
            result = default(string);
            errors = default(IEnumerable<IError>);
            return true;
        }
    }
}
