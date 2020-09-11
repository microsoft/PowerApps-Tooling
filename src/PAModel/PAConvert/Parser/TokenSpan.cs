// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Parser
{
    public struct TokenSpan
    {
        public readonly int Min;
        public readonly int Lim;

        public TokenSpan(int ichMin, int ichLim)
        {
            Min = ichMin;
            Lim = ichLim;
        }
    }
}
