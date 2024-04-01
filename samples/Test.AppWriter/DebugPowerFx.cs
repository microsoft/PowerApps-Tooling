// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Test.AppWriter;

internal class DebugPowerFx
{
    private RecalcEngine _engine;

    public DebugPowerFx(string fileName)
    {
        var config = new PowerFxConfig(Features.PowerFxV1);
        _engine = new RecalcEngine(config);
    }

    public void Run()
    {
        var result = _engine.Eval("Text(111)");

        if (result is ErrorValue errorValue)
            throw new Exception("Error: " + errorValue.Errors[0].Message);
        else
            Console.WriteLine(result);
    }
}
