using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class AddControl
    {
        public ControlPath ParentControlPath;
        public BlockNode Control;
        public ControlState ControlState;
    }
}
