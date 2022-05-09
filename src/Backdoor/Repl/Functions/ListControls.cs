// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Backdoor.Repl.Functions.ListFunctions;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Functions
{
    public class ListControls : IFunction<ICanvasDocument>
    {
        public string Name => "list";

        public delegate string ListFunction(ICanvasDocument document, IEnumerable<string> args, out IEnumerable<IError> errors);

        private IEnumerable<IFunction<ICanvasDocument>> _listFunctions = new List<IFunction<ICanvasDocument>>()
        {
            new ListScreens(),
            new ListChildren(),
        };

        public bool TryDo(ICanvasDocument thing, IEnumerable<string> args, out string result, out IEnumerable<IError> errors)
        {
            var argsList = args.ToList();
            var errorsList = new List<IError>();
            if (argsList.Count() < 1)
            {
                errorsList.Add(new BackdoorError(true, true, "Incorrect number of arguments."));
                errors = errorsList;
                result = default(string);
                return false;
            }

            // The valid list functions are themselves functions that are named for the second argument of list
            // and accept as arguments all arguments after the second argument provided to list.
            // E.g. The function for `list children test` is 'children' which accepts 'test' as an argument.
            var type = argsList.First();
            var function = _listFunctions.FirstOrDefault(function => function.Name == type);
            if (function == default(IFunction<ICanvasDocument>))
            {
                errorsList.Add(new BackdoorError(true, true, "Invalid argument"));
                errors = errorsList;
                result = default(string);
                return false;
            }

            var success = function.TryDo(thing, argsList.Skip(1), out result, out var functionErrors);
            if (functionErrors != null && functionErrors.Any())
                errorsList.AddRange(functionErrors);

            errors = errorsList;
            return success;
        }
    }
}
