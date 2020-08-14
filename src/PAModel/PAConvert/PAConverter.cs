using Microsoft.AppMagic.Authoring.Persistence;
using PAModel.PAConvert;
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
            // Ignore .pa file for now, use Json for roundtrip

            //var header = "//! PAFile:0.1";

            var text = File.ReadAllText(path);
            //if (!text.StartsWith(header))
            //{
            //    throw new InvalidOperationException($"Illegal pa source file. Missing header");
            //}
            //var json = text.Substring(header.Length);


            var control = JsonSerializer.Deserialize<ControlInfoJson>(text, Utility._jsonOpts);


            return SourceFile.New(control);
        }

        internal static string GetPAText(SourceFile sf)
        {
            ControlInfoJson control = sf.Value;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//! PAFile:0.1"); // some generic header

            new PAWriter(sb).WriteControl(control.TopParent);

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