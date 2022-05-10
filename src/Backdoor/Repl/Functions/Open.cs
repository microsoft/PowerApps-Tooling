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
    public class Open : IFunction<ICanvasDocument>
    {
        public string Name => "open";
        public IResult<ICanvasDocument> Invoke(ICanvasDocument thing, IEnumerable<string> args)
        {
            var location = args.FirstOrDefault();
            if (location == default(string))
            {
                return new ResultState<ICanvasDocument>(thing, new List<IError>()
                {
                    new BackdoorError(true, false, "No arguments!")
                });
            }

            var path = Path.GetFullPath(location);
            if (File.Exists(path))
            {
                (var msApp, var loadErrors) = Utility.TryOperation(() => CanvasDocument.LoadFromMsapp(path));
                return new ResultState<ICanvasDocument>(msApp, loadErrors, $"Successfully opened {location}");
            }
            else if (Directory.Exists(path))
            {
                (var msApp, var loadErrors) = Utility.TryOperation(() => CanvasDocument.LoadFromSources(path));
                return new ResultState<ICanvasDocument>(msApp, loadErrors, $"Successfully opened {location}");
            }

            return new ResultState<ICanvasDocument>(thing, new List<IError>()
            {
                new BackdoorError(true, false, $"{path} doesn't exist!")
            });
        }
    }
}
