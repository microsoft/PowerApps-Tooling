// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PAModelTests
{
    [TestClass]
    public class PublicSurfaceTests
    {
        [TestMethod]
        public void Test()
        {
            var asm = typeof(CanvasDocument).Assembly;

            var ns = "Microsoft.PowerPlatform.Formulas.Tools";
            HashSet<string> allowed = new HashSet<string>()
            {
                $"{ns}.{nameof(CanvasDocument)}",
                $"{ns}.{nameof(CanvasMerger)}",
                $"{ns}.{nameof(ChecksumMaker)}",
                $"{ns}.{nameof(ErrorContainer)}",
                $"{ns}.{nameof(Error)}",
                $"Microsoft.PowerPlatform.YamlConverter",
                $"Microsoft.PowerPlatform.YamlPocoSerializer",
                $"Microsoft.PowerPlatform.Formulas.Tools.Yaml.YamlWriter",
            };

            StringBuilder sb = new StringBuilder();
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic))
            {
                var name = type.FullName;
                if (!allowed.Contains(name))
                {
                    sb.Append(name);
                    sb.Append("; ");
                }

                allowed.Remove(name);
            }

            Assert.AreEqual(0, sb.Length, $"Unexpected public types: {sb}");

            // Types we expect to be in the assembly aren't there. 
            Assert.AreEqual(0, allowed.Count);
        }
    }
}

