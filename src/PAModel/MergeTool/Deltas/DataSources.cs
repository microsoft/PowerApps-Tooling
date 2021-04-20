using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class AddDataSource : IDelta
    {
        public string Name;
        public List<DataSourceEntry> Contents;

        public void Apply(CanvasDocument document)
        {
            if (document._dataSources.ContainsKey(Name))
                return;

            document._dataSources.Add(Name, Contents);
        }
    }
}
