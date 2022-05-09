// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Functions.ListFunctions
{
    public class ListScreens : IFunction<ICanvasDocument>
    {
        public string Name => "screens";
        public bool TryDo(ICanvasDocument thing, IEnumerable<string> args, out string result, out IEnumerable<IError> errors)
        {
            errors = default(IEnumerable<IError>);
            result = String.Join("\n", thing.Screens);
            return true;
        }
    }
}
