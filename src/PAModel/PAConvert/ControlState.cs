using System;
using System.Collections.Generic;
using System.Text;

namespace PAModel.PAConvert
{
    public class ControlState
    {
        public string Name { get; set; }
        public string ControlUniqueId { get; set; }
        public int Index { get; set; } = 0;
        public int PublishOrderIndex { get; set; } = 0; // 0 for group controls, increments in order otherwise
        public string LayoutName { get; set; } = "";
    }
}
