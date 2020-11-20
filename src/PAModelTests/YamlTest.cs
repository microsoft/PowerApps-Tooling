// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

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

        // These values should get automatically multiline escaped. 
        [DataTestMethod]
        [DataRow("Hi #There")] // Yaml comments are dangerous
        [DataRow("abc\r\ndef")]
        [DataRow("Patched({a : b})")]
        public void WriteEscapes(string value)
        {
            var sw = new StringWriter();
            var yw = new YamlWriter(sw);

            yw.WriteProperty("Foo", value);

            var text = sw.ToString();

            // We use a | escape. 
            Assert.IsTrue(text.StartsWith("Foo: |"));
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
        [DataRow("Hi #There")] // Yaml comments are dangerous
        [DataRow("abc\r\ndef")]
        [DataRow("Patched({a : b})")]
        public void NewLinesRoundtrip(string value)
        {
            var sw = new StringWriter();
            var yw = new YamlWriter(sw);
            yw.WriteProperty("Foo", value);

            var text = sw.ToString();

            // Validate it passes YamlDotNet
            var valueFromYaml = ParseSinglePropertyViaYamlDotNot(text);
            var valueWithoutR = value.Replace("\r", ""); // yamlDotNet doesn't do \R
            Assert.AreEqual(valueWithoutR, valueFromYaml);            

            // Validate it passes our subset. 
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
        [DataRow("Foo: =x #comment")] // comments not allowed in single line.
        [DataRow("Foo: |x\n  next")] // chars on same line after |
        [DataRow("Foo: >\n  next")] // > multiline not supported
        [DataRow("Foo: |\nBar: next")] // empty multiline
        [DataRow("---")] // multi docs not supported
        public void ExpectedError(string expr)
        {
            var sr = new StringReader(expr);
            var y = new YamlLexer(sr);

            AssertLexError(y);
        }

        // Error on 2nd token read. 
        [DataTestMethod]
        [DataRow("Foo:\r\n  val\r\n")] // Must have escape if there's a newline
        [DataRow("Foo:\r\nBar:\r\n")] // null prop not supported. Don't realize it until Bar:
        public void ExpectedError2(string expr)
        {
            var sr = new StringReader(expr);
            var y = new YamlLexer(sr);

            var tokenOk = y.ReadNext();
            Assert.AreNotEqual(YamlTokenKind.Error, tokenOk.Kind);

            AssertLexError(y);
        }

        // Yaml ignores duplicate properties. This could lead to data loss!
        // The lexer here catches duplicates and errors. 
        [TestMethod]
        public void ErrorOnDuplicate()
        {
            var text =
@"P1: =123
Obj1:
  P1: =Nested object, not duplicate
p1: =Casing Different, not duplicate
P2: =456
P1: =duplicate!
";
            var sr = new StringReader(text);
            var y = new YamlLexer(sr);

            AssertLex("P1=123", y);
            AssertLex("Obj1:", y);
                AssertLex("P1=Nested object, not duplicate", y);
            AssertLexEndObj(y);
            AssertLex("p1=Casing Different, not duplicate", y);
            AssertLex("P2=456", y);

            AssertLexError(y);            
        }

        [TestMethod]
        public void BasicRead()
        {
            var text =
@"P1: =123
P2: =456
";
            var sr = new StringReader(text);
            var y = new YamlLexer(sr);

            AssertLex("P1=123", y);
            AssertLex("P2=456", y);
            AssertLexEndFile(y);
            AssertLexEndFile(y);
        }

        // Test basic read of multiline
        [TestMethod]
        public void BasicMultiline()
        {
            var text = 
@"M1: |
    abc
     def
P1: =123
";
            var sr = new StringReader(text);
            var y = new YamlLexer(sr);
        
            AssertLex("M1=abc\r\n def\r\n", y);
            AssertLex("P1=123", y);
            AssertLexEndFile(y);
        }

        // Ensure we can get multiple EndObj tokens in a row. 
        [TestMethod]
        public void Closing()
        {
            var text = 
@"P0: =1
Obj1:
  Obj2:
    P1: =1

    P2: =2
P3: =3
";
            var sr = new StringReader(text);
            var y = new YamlLexer(sr);

            AssertLex("P0=1", y);
            AssertLex("Obj1:", y);
            AssertLex("Obj2:", y);
            AssertLex("P1=1", y);
            AssertLex("P2=2", y); // the newline prior isn't a token. 
            AssertLexEndObj(y); // Obj2
            AssertLexEndObj(y); // Obj1
            AssertLex("P3=3", y);
            AssertLexEndFile(y);
        }

        [TestMethod]
        public void T2()
        {
            var text = 
@"P0: =123
Obj1:
  P1a: =ABC
  Obj2:
    P2a: =X
    P2b: =Y
    P2c: =Z
  P1b: =DEF
";
            var sr = new StringReader(text);
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

        static void AssertLexError(YamlLexer y)
        {
            var p = y.ReadNext();
            Assert.AreEqual(YamlTokenKind.Error, p.Kind);
        }

        static void AssertLex(string expected, YamlLexer y)
        {
            var p = y.ReadNext();
            Assert.AreEqual(expected, p.ToString());
        }

        private static YamlToken[] ReadAllTokens(string text)
        {
            var sr = new StringReader(text);
            var y = new YamlLexer(sr);

            List<YamlToken> tokens = new List<YamlToken>();
            YamlToken token;
            do
            {
                token = y.ReadNext();
                tokens.Add(token);
                Assert.AreNotEqual(YamlTokenKind.Error, token.Kind);

                // Fragments are small. If we don't terminate, there's a parser bug. 
                Assert.IsTrue(tokens.Count < 100, "fragment failed to parse to EOF");
            } while (token.Kind != YamlTokenKind.EndOfFile);
            return tokens.ToArray();
        }

        // Parse a single property "Foo" expression with YamlDotNet. 
        private static string ParseSinglePropertyViaYamlDotNot(string text)
        {
            var deserializer = new DeserializerBuilder().Build();
            var o2 = (Dictionary<object, object>)deserializer.Deserialize(new StringReader(text));
            var val = (string)o2["Foo"];
            // Strip the '=' that we add.
            if (val[0] == '=')
            {
                val = val.Substring(1);
            }
            return val;
        }
        #endregion

    }


}
