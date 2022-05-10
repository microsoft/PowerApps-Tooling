// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace Backdoor.Repl.Functions.ListFunctions
{
    public class ListScreens : IFunction<ICanvasDocument>
    {
        public string Name => "screens";
        public IResult<ICanvasDocument> Invoke(ICanvasDocument thing, IEnumerable<string> args) =>
            new ResultState<ICanvasDocument>(thing, null, String.Join("\n", thing.Screens));
    }
}
