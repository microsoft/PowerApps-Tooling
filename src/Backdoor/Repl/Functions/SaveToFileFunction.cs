// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backdoor.Util;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Functions
{
    public abstract class SaveToFileFunction : IFunction<ICanvasDocument>
    {
        public abstract IEnumerable<IError> SaveToFile(ICanvasDocument document, string fullPathToMsApp);

        public abstract string Name { get; }

        public IResult<ICanvasDocument> Invoke(ICanvasDocument thing, IEnumerable<string> args)
        {
            var argsList = args.ToList();
            if (argsList.Count() == 0)
            {
                return new ResultState<ICanvasDocument>(thing, new List<IError>()
                {
                    new BackdoorError(true, false, "No arguments!")
                });
            }

            var path =  Path.GetFullPath(argsList.First());
            if (File.Exists(path))
            {
                return new ResultState<ICanvasDocument>(thing, new List<IError>()
                {
                    new BackdoorError(true, false, $"{path} exists!")
                });
            }

            var errors = Utility.TryOperation(() => SaveToFile(thing, path));
            var message = $"File saved at {path}";
            return new ResultState<ICanvasDocument>(thing, errors, message);
        }
    }
}
