// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backdoor.Repl;
using Backdoor.Repl.Menus;
using Backdoor.Util;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

Console.CancelKeyPress += (sender, cancelArgs) =>
{
    if (cancelArgs.SpecialKey == ConsoleSpecialKey.ControlC)
    {
        cancelArgs.Cancel = true;
        Environment.Exit(0);
    }
};

if (args == null || args.Length != 1)
{
    Console.WriteLine("Input only a single argument");
    return;
}

var msAppPath = Path.GetFullPath(args[0]);
if (!File.Exists(msAppPath))
{
    Console.WriteLine("The file you have input does not exist");
    return;
}

Console.WriteLine($"Please wait while {args[0]} is being unpacked...");
(ICanvasDocument msapp, IEnumerable<IError> errors) = Utility.TryOperation(() => CanvasDocument.LoadFromMsapp(msAppPath));
Console.Clear();

if (ReportErrors(errors)) return;

Repl<ICanvasDocument>.Start(new ReplMenu(), msapp);

// Reports errors from unpack operation to console
static bool ReportErrors(IEnumerable<IError> enumerable)
{
    var errorsArray = enumerable.ToArray();
    if (errorsArray.Any())
    {
        Console.WriteLine($"{errorsArray.Count()} errors encountered:");
        for (var i = 0; i < errorsArray.Length; i++)
        {
            Console.WriteLine($"{i}:  {errorsArray[i]}");
        }

        return true;
    }

    return false;
}
