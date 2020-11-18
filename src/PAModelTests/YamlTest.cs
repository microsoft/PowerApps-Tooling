// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace PAModelTests
{
    [TestClass]
    public class YamlTest
    {
        [TestMethod]
        public void Write1()
        {
            var sw = new StringWriter();
            var yw = new YamlWriter(sw);
            yw.WriteProperty("P0", "abc");
            yw.WriteStartObject("Obj1");
                yw.WriteProperty("P1a", "A");
                yw.WriteProperty("P1b", "B");
                yw.WriteStartObject("Obj2");
                    yw.WriteProperty("P2a", "A");
                    yw.WriteEndObject();
                yw.WriteProperty("P1c", "C");
                yw.WriteEndObject();

            var t = sw.ToString();
            Assert.AreEqual(
@"P0: =abc
Obj1:
  P1a: =A
  P1b: =B
  Obj2:
    P2a: =A
  P1c: =C
", t);

        }

        // Different ending newlines will have different escapes. 
        [DataTestMethod]
        [DataRow("\"brows_4.0\"")]
        [DataRow("a # b")] // Test with yaml comment. 
        [DataRow("x")] // easy, no newlines. 
        [DataRow("1\r\n2")] // multiline
        [DataRow("1\r\n2\r\n")] // multiline, trailing newline
        [DataRow("1 \r\n2 \r\n")] 
        [DataRow("1\r\n 2 \r\n ")]
        [DataRow("1\r\n2 \r\n ")]
        // Test with just \n?
        public void NewLinesRoundtrip(string value)
        {
            var sw = new StringWriter();
            var yw = new YamlWriter(sw);
            yw.WriteProperty("Foo", value);

            var text = sw.ToString();
            var sr = new StringReader(text);
            var y = new YamlLexer(sr);
            var p = y.ReadNext();
            Assert.AreEqual(YamlTokenKind.Property, p.Kind);
            Assert.AreEqual("Foo", p.Property);

            Assert.AreEqual(value, p.Value);
        }

        // Error on 1st token read
        [DataTestMethod]
        [DataRow("Foo: 12")] // missing =
        [DataRow("Foo: =x #comment")] // comments not allowed
        [DataRow("Foo: |x\n  next")] // chars on same line after |
        [DataRow("Foo: >\n  next")] // > multiline not supported
        [DataRow("Foo: |\nBar: next")] // empty multiline
        [DataRow("---")] // multi docs not supported
        public void ExpectedError(string expr)
        {
            var sr = new StringReader(expr);
            var y = new YamlLexer(sr);
            var tokenError = y.ReadNext();

            Assert.AreEqual(YamlTokenKind.Error, tokenError.Kind);
        }

        // Error on 2nd token read. 
        [DataTestMethod]
        [DataRow("Foo:\r\nBar\r\n")] // null prop not supported
        [DataRow("p1: =1\r\n\r\np2: =2")] // extra multilines not supported.
        public void ExpectedError2(string expr)
        {
            var sr = new StringReader(expr);
            var y = new YamlLexer(sr);

            var tokenOk = y.ReadNext();
            Assert.AreNotEqual(YamlTokenKind.Error, tokenOk.Kind);

            var tokenError = y.ReadNext();
            Assert.AreEqual(YamlTokenKind.Error, tokenError.Kind);
        }


        [TestMethod]
        public void T1()
        {
            var sr = new StringReader(
@"P1: =123
P2: =456
");
            var y = new YamlLexer(sr);

            AssertLex("P1=123", y);
            AssertLex("P2=456", y);
            AssertLexEndFile(y);
            AssertLexEndFile(y);
        }

        [TestMethod]
        public void Multiline()
        {
            var sr = new StringReader(
@"M1: |
    abc
     def
P1: =123
");
            var y = new YamlLexer(sr);
        
            AssertLex("M1=abc\r\n def\r\n", y);
            AssertLex("P1=123", y);
            AssertLexEndFile(y);
        }

        // Ensure we can get multiple EndObj tokens. 
        [TestMethod]
        public void Closing()
        {
            var sr = new StringReader(
@"P0: =1
Obj1:
  Obj2:
    P1: =1
    P2: =2
P3: =3
");
            var y = new YamlLexer(sr);

            AssertLex("P0=1", y);
            AssertLex("Obj1:", y);
            AssertLex("Obj2:", y);
            AssertLex("P1=1", y);
            AssertLex("P2=2", y);
            AssertLexEndObj(y); // Obj2
            AssertLexEndObj(y); // Obj1
            AssertLex("P3=3", y);
            AssertLexEndFile(y);
        }

        [TestMethod]
        public void T2()
        {
            var sr = new StringReader(
@"P0: =123
Obj1:
  P1a: =ABC
  Obj2:
    P2a: =X
    P2b: =Y
    P2c: =Z
  P1b: =DEF
");
            var y = new YamlLexer(sr);

            AssertLex("P0=123", y);
            AssertLex("Obj1:", y);
                AssertLex("P1a=ABC", y);
                AssertLex("Obj2:", y);
                    AssertLex("P2a=X", y);
                    AssertLex("P2b=Y", y);
                    AssertLex("P2c=Z", y);
                    AssertLexEndObj(y); // Obj2
                AssertLex("P1b=DEF", y);
            AssertLexEndObj(y); // Obj1
            AssertLexEndFile(y);
        }

        #region Helpers
        static void AssertLexEndFile(YamlLexer y)
        {
            AssertLex("<EndOfFile>", y);
        }

        static void AssertLexEndObj(YamlLexer y)
        {
            AssertLex("<EndObj>", y);
        }
        static void AssertLex(string expected, YamlLexer y)
        {
            var p = y.ReadNext();
            Assert.AreEqual(expected, p.ToString());
        }
        #endregion

    }


}
