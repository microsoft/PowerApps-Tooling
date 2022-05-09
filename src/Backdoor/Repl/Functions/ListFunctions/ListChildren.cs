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
        public bool TryDo(ICanvasDocument thing, IEnumerable<string> args, out string result, out IEnumerable<IError> errors)
        {
            var argsList = args.ToList();
            var errorsList = new List<IError>();
            var parent = argsList.FirstOrDefault();
            if (parent == default(string))
            {
                errorsList.Add(new BackdoorError(true, false, "No arguments provided"));
                errors = errorsList;
                result = default(string);
                return false;
            }

            if (!thing.Exists(parent))
            {
                errorsList.Add(new BackdoorError(true, false, $"{parent} does not exist"));
                errors = errorsList;
                result = default(string);
                return false;
            }

            errors = errorsList;
            result = String.Join("\n", thing.GetChildren(parent));
            return true;
        }
    }
}
