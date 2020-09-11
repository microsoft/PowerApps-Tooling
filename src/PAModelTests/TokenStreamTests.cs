// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace PAModelTests
{
    [TestClass]
    public class TokenStreamTests
    {
        [TestMethod]
        public void TestIndentIncrease()
        {
            var testString =
@"
A
    B
        C
";

            var tokStream = new TokenStream(testString);
            AreEqual(tokStream, new List<Token>()
            {
                new Token(TokenKind.Identifier, new TokenSpan(2,3), "A"),
                new Token(TokenKind.Indent, new TokenSpan(5,9), "    "),
                new Token(TokenKind.Identifier, new TokenSpan(9,10), "B"),
                new Token(TokenKind.Indent, new TokenSpan(12,20), "        "),
                new Token(TokenKind.Identifier, new TokenSpan(20,21), "C"),
            });
        }

        [TestMethod]
        public void TestIndentDecrease()
        {
            var testString =
@"
A
    B
        C
D
";

            var tokStream = new TokenStream(testString);
            AreEqual(tokStream, new List<Token>()
            {
                new Token(TokenKind.Identifier, new TokenSpan(2,3), "A"),
                new Token(TokenKind.Indent, new TokenSpan(5,9), "    "),
                new Token(TokenKind.Identifier, new TokenSpan(9,10), "B"),
                new Token(TokenKind.Indent, new TokenSpan(12,20), "        "),
                new Token(TokenKind.Identifier, new TokenSpan(20,21), "C"),
                new Token(TokenKind.Dedent, new TokenSpan(23,23), ""),
                new Token(TokenKind.Dedent, new TokenSpan(23,23), ""),
                new Token(TokenKind.Identifier, new TokenSpan(23,24), "D"),
            });
        }



        private void AreEqual(TokenStream stream, List<Token> expectedTokens)
        {
            foreach (var token in expectedTokens)
            {
                var actualToken = stream.GetNextToken();

                Assert.AreEqual(token, actualToken);
            }
        }
    }
}
