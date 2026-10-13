// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using PAParser = Microsoft.PowerPlatform.Formulas.Tools.Parser.Parser;

namespace PAModelTests;

[TestClass]
public class PAWriterVisitorTests
{
    [TestMethod]
    public void ArgMetadataBlock_SimpleIdentifier_IsWrittenUnquotedAndRoundTrips()
    {
        var ir = CreateComponentWithFunction(
            Metadata("StartDate", "Now()"),
            Metadata(PAConstants.ThisPropertyIdentifier, "true"));

        var yaml = PAWriterVisitor.PrettyPrint(ir);

        var expected = string.Join("\n",
            "Component1 As CanvasComponent:",
            "    ValidateDates(StartDate As DateTime):",
            "        StartDate:",
            "            Default: =Now()",
            "        ThisProperty:",
            "            Default: =true",
            "    Fill: =RGBA(0, 0, 0, 0)",
            "",
            "");
        Assert.AreEqual(expected, yaml.Replace("\r\n", "\n"));

        var metadata = ParseFunctionMetadata(yaml);
        Assert.HasCount(2, metadata);
        Assert.AreEqual("StartDate", metadata[0].Identifier);
        Assert.AreEqual("Now()", metadata[0].Default.Expression);
        Assert.AreEqual(PAConstants.ThisPropertyIdentifier, metadata[1].Identifier);
        Assert.AreEqual("true", metadata[1].Default.Expression);
    }

    [TestMethod]
    [DataRow("my arg")]
    [DataRow("a:b")]
    [DataRow("a: b")]
    [DataRow("it's")]
    [DataRow("'quoted'")]
    [DataRow("\"double\"")]
    [DataRow("#hash")]
    [DataRow("1abc")]
    public void ArgMetadataBlock_IdentifierRequiringEscaping_RoundTrips(string identifier)
    {
        var ir = CreateComponentWithFunction(
            Metadata(identifier, "Now()"),
            Metadata(PAConstants.ThisPropertyIdentifier, "true"));

        var yaml = PAWriterVisitor.PrettyPrint(ir);

        var metadata = ParseFunctionMetadata(yaml);
        Assert.HasCount(2, metadata);
        Assert.AreEqual(identifier, metadata[0].Identifier);
        Assert.AreEqual("Now()", metadata[0].Default.Expression);
        Assert.AreEqual(PAConstants.ThisPropertyIdentifier, metadata[1].Identifier);
        Assert.AreEqual("true", metadata[1].Default.Expression);
    }

    [TestMethod]
    [DataRow("a\nb")]
    [DataRow("a\r\nb")]
    [DataRow("a\rb")]
    [DataRow("a:\nb")]
    [DataRow("a\u0085b")]
    [DataRow("a\u2028b")]
    [DataRow("a\u2029b")]
    public void ArgMetadataBlock_IdentifierWithLineBreaks_IsRejected(string identifier)
    {
        var ir = CreateComponentWithFunction(
            Metadata(identifier, "Now()"),
            Metadata(PAConstants.ThisPropertyIdentifier, "true"));

        Assert.ThrowsExactly<ArgumentException>(() => PAWriterVisitor.PrettyPrint(ir));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("''")]
    [DataRow("\"'\"")]
    public void ArgMetadataBlock_EmptyIdentifier_ReportsParseError(string key)
    {
        var yaml = string.Join("\n",
            "Component1 As CanvasComponent:",
            "    ValidateDates(StartDate As DateTime):",
            $"        {key}:",
            "            Default: =Now()",
            "");

        var errors = new ErrorContainer();
        using var parser = new PAParser("test.fx.yaml", yaml, errors);

        Assert.ThrowsExactly<DocumentException>(() => parser.ParseControl(isComponent: true));
        Assert.IsTrue(errors.HasErrors);
    }

    private static ArgMetadataBlockNode Metadata(string identifier, string defaultExpression)
    {
        return new ArgMetadataBlockNode
        {
            Identifier = identifier,
            Default = new ExpressionNode { Expression = defaultExpression },
        };
    }

    private static BlockNode CreateComponentWithFunction(params ArgMetadataBlockNode[] metadata)
    {
        return new BlockNode
        {
            Name = new TypedNameNode
            {
                Identifier = "Component1",
                Kind = new TypeNode { TypeName = "CanvasComponent" },
            },
            Functions =
            [
                new FunctionNode
                {
                    Identifier = "ValidateDates",
                    Args =
                    [
                        new TypedNameNode
                        {
                            Identifier = "StartDate",
                            Kind = new TypeNode { TypeName = "DateTime" },
                        },
                    ],
                    Metadata = [.. metadata],
                },
            ],
            Properties =
            [
                new PropertyNode
                {
                    Identifier = "Fill",
                    Expression = new ExpressionNode { Expression = "RGBA(0, 0, 0, 0)" },
                },
            ],
        };
    }

    private static IList<ArgMetadataBlockNode> ParseFunctionMetadata(string yaml)
    {
        var errors = new ErrorContainer();
        using var parser = new PAParser("test.fx.yaml", yaml, errors);

        var block = parser.ParseControl(isComponent: true);

        Assert.IsFalse(errors.HasErrors, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
        Assert.IsNotNull(block);
        Assert.HasCount(1, block.Functions);
        Assert.HasCount(1, block.Properties);
        Assert.AreEqual("Fill", block.Properties[0].Identifier);
        return block.Functions[0].Metadata;
    }
}
