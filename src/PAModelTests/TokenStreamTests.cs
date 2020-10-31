// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
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

            var tokStream = new TokenStream(testString, "foo");
            AreEqual(tokStream, new List<Token>()
            {
                new Token(TokenKind.Identifier, new SourceLocation(1,1,1,2, "foo"), "A"),
                new Token(TokenKind.Indent, new SourceLocation(2,1,2,5, "foo"), "    "),
                new Token(TokenKind.Identifier, new SourceLocation(2,5,2,6, "foo"), "B"),
                new Token(TokenKind.Indent, new SourceLocation(3,1,3,9, "foo"), "        "),
                new Token(TokenKind.Identifier, new SourceLocation(3,9,3,10, "foo"), "C"),
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

            var tokStream = new TokenStream(testString, "foo");
            AreEqual(tokStream, new List<Token>()
            {
                new Token(TokenKind.Identifier, new SourceLocation(1,1,1,2, "foo"), "A"),
                new Token(TokenKind.Indent, new SourceLocation(2,1,2,5, "foo"), "    "),
                new Token(TokenKind.Identifier, new SourceLocation(2,5,2,6, "foo"), "B"),
                new Token(TokenKind.Indent, new SourceLocation(3,1,3,9, "foo"), "        "),
                new Token(TokenKind.Identifier, new SourceLocation(3,9,3,10, "foo"), "C"),
                new Token(TokenKind.Dedent, new SourceLocation(4,1,4,1, "foo"), ""),
                new Token(TokenKind.Dedent, new SourceLocation(4,1,4,1, "foo"), ""),
                new Token(TokenKind.Identifier, new SourceLocation(4,1,4,2, "foo"), "D"),
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
