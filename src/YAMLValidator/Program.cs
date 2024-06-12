// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        var inputProcessor = InputProcessor.GetRootCommand();
        inputProcessor.Invoke(args);
    }
}
