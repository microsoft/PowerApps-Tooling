// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PAModelTests
{
    [TestClass]
    public class ErrorTests
    {
        [TestMethod]
        public void Test()
        {
            (var doc, var errors) = CanvasDocument.LoadFromMsapp(@"c:\missing");
            Assert.IsTrue(errors.HasErrors);
            Assert.IsNull(doc);
        }
    }
}
