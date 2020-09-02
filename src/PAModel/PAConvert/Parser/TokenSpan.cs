using System;
using System.Collections.Generic;
using System.Text;

namespace PAModel.PAConvert.Parser
{
    internal struct TokenSpan
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
