// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.JsonConverters;

namespace PAModelTests;

[TestClass]
public class JsonNormalizerTests
{
    [TestMethod]
    public void Test()
    {
        // - Property ordering             
        // - Canonical whitespace
        // - number encoding 
        var str1 = JsonNormalizer.Normalize(/*lang=json*/ "{ \"A\"     : 12.0, \"B\" \r\n: 34} ");
        var str2 = JsonNormalizer.Normalize(/*lang=json,strict*/ "{ \"B\" : 34, \"A\" : 12} ");

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
