// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal static class PAConverter
    {
        internal static string GetPAText(SourceFile sf)
        {
            ControlInfoJson control = sf.Value;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//! PAFile:0.1"); // some generic header

            new PAWriter(sb).WriteControl(control.TopParent, sf.Kind != SourceKind.Control);

            return sb.ToString();
        }

        private static IEnumerable<ControlInfoJson.RuleEntry> Formulas(ControlInfoJson.Item item)
        {
            foreach(var rule in item.Rules)
            {
                yield return rule;
            }

            foreach(var child in item.Children)
            {
                foreach(var rule in Formulas(child))
                {
                    yield return rule;
                }
            }
        }
    }
}