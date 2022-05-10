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
            new Open(),
            new Delete(),
            new Pack(),
            new ToSource(),
            new ListControls(),
            new Exit()
        };

        public IMenuResultState<ICanvasDocument> TransferFunction(string input, ICanvasDocument document)
        {
            var tokens = Parser.Tokenize(input).ToArray();
            var command = tokens.FirstOrDefault();
            if (command == null)
            {
                return new MenuResultState<ICanvasDocument>(this, document);
            }

            var function = FunctionLibrary.FirstOrDefault(function =>
            {
                return function.Name == command;
            });

            if (function == null)
            {
                return new MenuResultState<ICanvasDocument>(this, document, new List<IError>()
                {
                    new BackdoorError(true, false, $"{command} is not a valid command.")
                });
            }

            return MenuResultState<ICanvasDocument>.FromResultState(function.Invoke(document, tokens.Skip(1)), this);
        }

        public string Title => "";
        public string Description =>  "> ";
    }
}
