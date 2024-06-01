// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;

namespace YAMLValidator;

internal sealed class Program
{
    private static void Main(String[] args)
    {
        var inputProcessor = InputProcessor.GetRootCommand();
        inputProcessor.Invoke(args);
    }
}
