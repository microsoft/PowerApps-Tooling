using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PAModel
{
    internal static class PAConverter
    {
        internal static SourceFile ReadSource(string path)
        {
            var header = "//! PAFile:0.1";

            var text = File.ReadAllText(path);
            if (!text.StartsWith(header))
            {
                throw new InvalidOperationException($"Illegal pa source file. Missing header");
            }
            var json = text.Substring(header.Length);


            var control = JsonSerializer.Deserialize<ControlInfoJson>(json, Utility._jsonOpts);


            return SourceFile.New(control);
        }

        internal static string GetPAText(SourceFile sf)
        {
            ControlInfoJson control = sf.Value;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//! PAFile:0.1"); // some generic header

            var json = JsonSerializer.Serialize(sf.Value, Utility._jsonOpts);
            sb.AppendLine(json);

            /*
            sb.AppendLine("//! Kind:" + sf.Kind);
            sb.AppendLine("//! TemplateName:" + sf.TemplateName);
            sb.AppendLine("//! Locale:Invariant");
            sb.AppendLine();

            // $$$ Todo, use a real .pa format. 
            foreach(var rule in Formulas(control.TopParent) )
            {
                sb.AppendLine($"{rule.Property} := {rule.InvariantScript}");

                sb.AppendLine();
            }*/

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