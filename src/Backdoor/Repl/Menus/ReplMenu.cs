// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Backdoor.Repl.Functions;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Menus
{
    public class ReplMenu : IMenu<ICanvasDocument>
    {
        public static IList<IFunction<ICanvasDocument>> FunctionLibrary { get; } = new List<IFunction<ICanvasDocument>>()
        {
            new Delete(),
            new Pack(),
            new ToSource(),
            new ListControls(),
            new Exit()
        };

        public (IMenu<ICanvasDocument>, string) TransferFunction(string input, ICanvasDocument document, out IEnumerable<IError> errors)
        {
            var tokens = Parser.Tokenize(input).ToArray();
            var command = tokens.FirstOrDefault();
            var errorsList = new List<IError>();
            if (command == null)
            {
                errors = errorsList;
                return (this, default(string));
            }

            var function = FunctionLibrary.FirstOrDefault(function =>
            {
                return function.Name == command;
            });

            if (function == null)
            {
                errorsList.Add(new BackdoorError(true, false, $"{command} is not a valid command."));
                errors = errorsList;
                return (this, default(string));
            }

            if (!function.TryDo(document, tokens.Skip(1), out var result, out var functionErrors))
            {
                errorsList.Add(new BackdoorError(true, false, $"Command {function.Name} failed."));
                errorsList.AddRange(functionErrors);
            }

            errors = errorsList;
            return (this, result);
        }

        public string Title => "";
        public string Description =>  "> ";
    }
}
