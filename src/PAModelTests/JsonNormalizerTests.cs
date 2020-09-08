using Microsoft.VisualStudio.TestTools.UnitTesting;
using PAModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace PAModelTests
{
    [TestClass]
    public class JsonNormalizerTests
    {
        [TestMethod]
        public void Test()
        {
            // - Property ordering             
            // - Canonical whitespace
            // - number encoding 
            var str1 = JsonNormalizer.Normalize("{ \"A\"     : 12.0, \"B\" \r\n: 34} ");
            var str2 = JsonNormalizer.Normalize("{ \"B\" : 34, \"A\" : 12} ");

            Assert.AreEqual(str1, str2);
        }

        // String escaping normalizing. \u is an escape, Multiple ways to encode the same char.
        [DataTestMethod]
        [DataRow("\"a\\\"bc\"")]
        [DataRow("\"a\\u0022bc\"")]
        public void StringEncoding(string unescaped)
        {
            var norm = JsonNormalizer.Normalize(unescaped);
            var expected = "\"a\\\"bc\"";
            Assert.AreEqual(expected, norm);
        }
    }
}
