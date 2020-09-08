// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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