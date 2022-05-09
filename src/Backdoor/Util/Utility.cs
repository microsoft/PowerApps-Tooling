// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Util
{
    public class Utility
    {
        public static (ICanvasDocument, IEnumerable<IError>) TryOperation(Func<(ICanvasDocument, IEnumerable<IError>)> operation)
        {
            try
            {
                return operation();
            }
            catch (Exception e)
            {
                // Add unhandled exception to the error container.
                return (null, new []{ new BackdoorError(true, false, $"Internal error. {e.Message}\r\nStack Trace:\r\n{e.StackTrace}") });
            }
        }

        public static IEnumerable<IError> TryOperation(Func<IEnumerable<IError>> operation)
        {
            try
            {
                return operation();
            }
            catch (Exception e)
            {
                // Add unhandled exception to the error container.
                return new []{ new BackdoorError(true, false, $"Internal error. {e.Message}\r\nStack Trace:\r\n{e.StackTrace}") };
            }
        }
    }
}
