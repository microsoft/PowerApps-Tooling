// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Functions.ListFunctions
{
    public class ListChildren : IFunction<ICanvasDocument>
    {
        public string Name => "children";
        public IResult<ICanvasDocument> Invoke(ICanvasDocument thing, IEnumerable<string> args)
        {
            var argsList = args.ToList();
            var errorsList = new List<IError>();
            var parent = argsList.FirstOrDefault();
            if (parent == default(string))
            {
                errorsList.Add(new BackdoorError(true, false, "No arguments provided"));
                return new ResultState<ICanvasDocument>(thing, errorsList);
            }

            if (!thing.Exists(parent))
            {
                errorsList.Add(new BackdoorError(true, false, $"{parent} does not exist"));
                return new ResultState<ICanvasDocument>(thing, errorsList);
            }

            var message = String.Join("\n", thing.GetChildren(parent));
            return new ResultState<ICanvasDocument>(thing, errorsList, message);
        }
    }
}
