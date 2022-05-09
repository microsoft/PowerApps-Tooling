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

        public bool TryDo(ICanvasDocument thing, IEnumerable<string> args, out string result, out IEnumerable<IError> errors)
        {
            var errorsList = new List<IError>();
            var argsList = args.ToList();
            if (argsList.Count() == 0)
            {
                errorsList.Add(new BackdoorError(true, false, "No arguments!"));
                errors = errorsList;

                result = default(string);
                return false;
            }

            var path =  Path.GetFullPath(argsList.First());
            if (File.Exists(path))
            {
                errorsList.Add(new BackdoorError(true, false, $"{path} exists!"));
                errors = errorsList;

                result = default(string);
                return false;
            }

            errors = Utility.TryOperation(() => SaveToFile(thing, path));
            result = $"File saved at {path}";
            return !errors.Any(error => error.IsError);
        }
    }
}
