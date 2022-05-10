// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Functions
{
    public class Delete : IFunction<ICanvasDocument>
    {
        public string Name => "delete";
        public IResult<ICanvasDocument> Invoke(ICanvasDocument thing, IEnumerable<string> args)
        {
            // To avoid errors, we validate the input before mutating the document
            var errorsList = new List<IError>();
            foreach (var arg in args)
            {
                if (!thing.Exists(arg))
                {
                    errorsList.Add(new BackdoorError(true, false, $"{arg} does not exist"));
                }
            }

            if (errorsList.Any())
            {
                return new ResultState<ICanvasDocument>(thing, errorsList);
            }

            foreach (var arg in args)
            {
                if (!thing.TryRemoveControl(arg, out var deleteErrors))
                {
                    errorsList.Add(new BackdoorError(false, true, $"Control {arg} was located but could not be removed."));
                    if (deleteErrors != null && deleteErrors.Any())
                        errorsList.AddRange(deleteErrors);
                }
            }

            return new ResultState<ICanvasDocument>(thing, errorsList);
        }
    }
}
