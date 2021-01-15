// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace PAModelTests
{
    [TestClass]
    public class ParserTests
    {
        [DataTestMethod]
        [DataRow("Foo", true, "Foo", 3)]
        [DataRow("'Foo'", true, "Foo", 5)]
        [DataRow("'Foo Bar'", true, "Foo Bar", 9)]
        [DataRow("'Foo B''ar'", true, "Foo B'ar", 11)]
        [DataRow("'F''o''o B''ar'", true, "F'o'o B'ar", 15)]
        [DataRow("Foo Bar", true, "Foo", 3)]
        [DataRow("'Foo' Bar", true, "Foo", 5)]
        [DataRow("''", false, null, 0)]
        [DataRow("'Foo Bar", false, null, 0)]
        [DataRow("'Foo ''Bar", false, null, 0)]
        public void TestParseIdent(string input, bool shouldParse, string output, int expectedLength)
        {
            Assert.AreEqual(shouldParse, Parser.TryParseIdent(input, out var ident, out var length));
            Assert.AreEqual(output, ident);
            Assert.AreEqual(expectedLength, length);
        }

        [DataTestMethod]
        [DataRow("Foo As Bar", true, "Foo", "Bar", null)]
        [DataRow("Foo As Bar.Baz", true, "Foo", "Bar", "Baz")]
        [DataRow("'escaped foo' As Bar", true, "escaped foo", "Bar", null)]
        [DataRow("'escaped foo' As Bar.'escaped'", true, "escaped foo", "Bar", "escaped")]
        [DataRow("'es''caped f''oo' As 'Escaped'.foo", true, "es'caped f'oo", "Escaped", "foo")]
        public void TestParseControlDef(string input, bool shouldParse, string control, string type, string variant)
        {
            Assert.AreEqual(shouldParse, Parser.TryParseControlDefCore(input, out var cActual, out var tActual, out var vActual));
            Assert.AreEqual(control, cActual);
            Assert.AreEqual(type, tActual);
            Assert.AreEqual(variant, vActual);

        }
    }
}
