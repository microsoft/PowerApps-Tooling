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
                new Token(TokenKind.Identifier, new TokenSpan(1, 2), "A"),
                new Token(TokenKind.Indent, new TokenSpan(3, 7), "    "),
                new Token(TokenKind.Identifier, new TokenSpan(7, 8), "B"),
                new Token(TokenKind.Indent, new TokenSpan(9, 17), "        "),
                new Token(TokenKind.Identifier, new TokenSpan(17, 18), "C"),
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
                new Token(TokenKind.Identifier, new TokenSpan(1, 2), "A"),
                new Token(TokenKind.Indent, new TokenSpan(3, 7), "    "),
                new Token(TokenKind.Identifier, new TokenSpan(7, 8), "B"),
                new Token(TokenKind.Indent, new TokenSpan(9, 17), "        "),
                new Token(TokenKind.Identifier, new TokenSpan(17, 18), "C"),
                new Token(TokenKind.Dedent, new TokenSpan(19, 19), ""),
                new Token(TokenKind.Dedent, new TokenSpan(19, 19), ""),
                new Token(TokenKind.Identifier, new TokenSpan(19, 20), "D"),
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
