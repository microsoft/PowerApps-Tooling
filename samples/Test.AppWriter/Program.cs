// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;

namespace Test.AppWriter;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        var rootCommand = InputProcessor.GetRootCommand();

        rootCommand.Invoke(args);
    }
}
