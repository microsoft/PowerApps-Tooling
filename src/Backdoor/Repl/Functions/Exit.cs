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
        public IResult<ICanvasDocument> Invoke(ICanvasDocument thing, IEnumerable<string> args)
        {
            Environment.Exit(0);
            return new ResultState<ICanvasDocument>(thing);
        }
    }
}
