// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Utility
{
    internal interface ICloneable<T>
    {
        T Clone();
    }
}
