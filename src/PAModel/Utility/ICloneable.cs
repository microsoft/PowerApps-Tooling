using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Utility
{
    public interface ICloneable<T>
    {
        T Clone();
    }
}
