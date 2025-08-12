// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions.Execution;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using YamlDotNet.Serialization;

namespace Persistence.Tests.PaYaml.Serialization;

[TestClass]
public class PFxExpressionYamlConverterTests : SerializationTestBase
{
    private static readonly PFxExpressionYamlFormattingOptions FailSafeOptions = new()
    {
        ForceLiteralBlockIfContainsAny = [],
    };

    public PFxExpressionYamlConverterTests()
    {
        DefaultOptions = DefaultOptions with
        {
            PFxExpressionYamlFormatting = FailSafeOptions,
        };
    }

    protected override void ConfigureYamlDotNetDeserializer(DeserializerBuilder builder, PaYamlSerializationContext context)
    {
        base.ConfigureYamlDotNetDeserializer(builder, context);
        builder.WithTypeConverter(new PFxExpressionYamlConverter(context.Options.PFxExpressionYamlFormatting));
    }

    protected override void ConfigureYamlDotNetSerializer(SerializerBuilder builder, PaYamlSerializationContext context)
    {
        base.ConfigureYamlDotNetSerializer(builder, context);
        builder.WithTypeConverter(new PFxExpressionYamlConverter(context.Options.PFxExpressionYamlFormatting));
    }

    [TestMethod]
    public void ReadYamlWithFailSafeOptions()
    {
        // Null literals
        VerifyDeserialize("Expression: ", null);
        VerifyDeserialize("Expression:", null);
        VerifyDeserialize("Expression: ~", null);
        VerifyDeserialize("Expression: Null", null);

        // Plain scalars
        VerifyDeserialize("Expression: =foo", "foo");
        VerifyDeserialize("Expression: =null", "null");
        VerifyDeserialize("Expression: =\"Hello world!\"", "\"Hello world!\"");

        // Literal blocks
        VerifyDeserialize("Expression: |\n  =foo", "foo");
        VerifyDeserialize("Expression: |\n  =null", "null");
        VerifyDeserialize("Expression: |\n  =\"Hello world!\"", @"""Hello world!""");

        // Multiline scripts
        VerifyDeserialize("Expression: |-\n  =val1\n   & \n  foo", "val1\n & \nfoo");
        VerifyDeserialize("Expression: |\n  =val1\n   & \n  foo", "val1\n & \nfoo");
        VerifyDeserialize("Expression: |-\n  =val1\n   & \n  foo\n", "val1\n & \nfoo");
        VerifyDeserialize("Expression: |\n  =val1\n   & \n  foo\n", "val1\n & \nfoo\n");
        VerifyDeserialize("Expression: |-\n  =val1\n   & \n  foo\n  ", "val1\n & \nfoo");
        VerifyDeserialize("Expression: |\n  =val1\n   & \n  foo\n  ", "val1\n & \nfoo\n");
    }

    private void VerifyDeserialize(string yaml, string? expectedScript)
    {
        var testObject = DeserializeViaYamlDotNet<NamedPFxExpressionYaml>(yaml);
        testObject.ShouldNotBeNull();
        if (expectedScript is null)
        {
            testObject.Expression.Should().BeNull();
        }
        else
        {
            testObject.Expression.ShouldNotBeNull();
            testObject.Expression.InvariantScript.Should().Be(expectedScript);
        }
    }

    [TestMethod]
    public void WriteYamlWithFailSafeOptions()
    {
        using var _ = new AssertionScope();

        // Only the canonical null literal will be written:
        VerifySerialize("", "=");

        // Plain scalars
        VerifySerialize("foo", "=foo");
        VerifySerialize("null", "=null");
        VerifySerialize(@"""Hello world!""", "=\"Hello world!\"");

        // Forced to literal blocks
        VerifySerialize("Format(val, # has pound)", "|-\n  =Format(val, # has pound)");
        VerifySerialize("Format(val, : has colon)", "|-\n  =Format(val, : has colon)");

        // Multiline scripts
        VerifySerialize("val1\n & \nfoo", "|-\n  =val1\n   & \n  foo");
        VerifySerialize("val1\n & \nfoo\n", "|\n  =val1\n   & \n  foo");
    }

    [TestMethod]
    public void ReadYamlWithTrailingWhitespace()
    {
        using var _ = new AssertionScope();

        // Note: YAML ignores trailing spaces for plain scalars, so they are removed from parsed scalar values here:
        VerifyDeserialize("Expression: =val1 & foo   ", "val1 & foo");
        VerifyDeserialize("Expression: =val1 & foo\t", "val1 & foo");

        // Property encoded single-line ending with whitespace
        VerifyDeserialize("Expression: '=val1 & foo   '", "val1 & foo   ");
        VerifyDeserialize("Expression: '=val1 & foo\t'", "val1 & foo\t");
        VerifyDeserialize("Expression: \"=val1 & foo   \"", "val1 & foo   ");
        VerifyDeserialize("Expression: \"=val1 & foo\t\"", "val1 & foo\t");
        VerifyDeserialize("Expression: |-\n  =val1 & foo   ", "val1 & foo   ");
        VerifyDeserialize("Expression: |-\n  =val1 & foo\t", "val1 & foo\t");
    }

    [TestMethod]
    public void WriteYamlWithTrailingWhitespace()
    {
        using var _ = new AssertionScope();

        // single-line ending with whitespace
        VerifySerialize("val1 & foo   ", "'=val1 & foo   '");

        // BUG: YamlDotNet doesn't put single quotes around the value when it ends with a tab character
        // So our logic forces the quoting to happen if this is the case.
        VerifySerialize("val1 & foo  \t", "'=val1 & foo  \t'");
    }

    [TestMethod]
    public void WriteYamlWithEscapedStringInCodeView()
    {
        using var _ = new AssertionScope();

        VerifySerialize("Set(gblFoo, true);\n // The next line is completely blank:\n\nSet(gblBar, 42)",
            "|-\n  =Set(gblFoo, true);\n   // The next line is completely blank:\n\n  Set(gblBar, 42)");

        // BUG:31691580 YamlDotNet doesn't serialize string when spaces are added in blank lines and shows escaped string in codeview
        // LoadingPage.OnVisible with spaces on blank line in the middle, serialization adds \r\n for new lines on windows-os and \n for non-windows os


        VerifySerialize("Set(gblFoo,\n   \n   \"Hello world!\");",
            "\"=Set(gblFoo,\\n   \\n   \\\"Hello world!\\\");\"");

        VerifySerialize("Set(gblFoo, true);\n// The next line has one or more spaces:\n    \nSet(gblBar, 42);",
            "\"=Set(gblFoo, true);\\n// The next line has one or more spaces:\\n    \\nSet(gblBar, 42);\"");
    }

    [TestMethod]
    public void WriteYamlWithUnicodeCharacters()
    {
        using var _ = new AssertionScope();

        // Test Unicode characters in PowerFx expressions are preserved
        VerifySerialize("Set(用户名, \"John\")", "|-\n  =Set(用户名, \"John\")");
        VerifySerialize("Text(\"こんにちは世界\")", "|-\n  =Text(\"こんにちは世界\")");
        VerifySerialize("Format(价格, \"$#,##0.00\")", "|-\n  =Format(价格, \"$#,##0.00\")");

        // Test mixed Unicode and ASCII
        VerifySerialize("Concatenate(\"Hello \", 世界, \"!\")", "|-\n  =Concatenate(\"Hello \", 世界, \"!\")");

        // Test emoji and special Unicode characters
        VerifySerialize("Set(Status, \"✅ Complete\")", "|-\n  =Set(Status, \"✅ Complete\")");
        VerifySerialize("Text(\"温度: \" & Temp & \"℃\")", "|-\n  =Text(\"温度: \" & Temp & \"℃\")");

        // Test Unicode in variable names and strings
        VerifySerialize("Set(变量_测试, \"龭唉𫓧G㐁A𫟦D𠳐ⅷ𫇭C丂\")", "|-\n  =Set(变量_测试, \"龭唉𫓧G㐁A𫟦D𠳐ⅷ𫇭C丂\")");
    }

    [TestMethod]
    public void ReadYamlWithUnicodeCharacters()
    {
        using var _ = new AssertionScope();

        // Test Unicode characters can be deserialized from literal blocks
        VerifyDeserialize("Expression: |-\n  =Set(用户名, \"John\")", "Set(用户名, \"John\")");
        VerifyDeserialize("Expression: |-\n  =Text(\"こんにちは世界\")", "Text(\"こんにちは世界\")");
        VerifyDeserialize("Expression: |-\n  =Format(价格, \"$#,##0.00\")", "Format(价格, \"$#,##0.00\")");

        // Test mixed Unicode and ASCII
        VerifyDeserialize("Expression: |-\n  =Concatenate(\"Hello \", 世界, \"!\")", "Concatenate(\"Hello \", 世界, \"!\")");

        // Test emoji and special Unicode characters
        VerifyDeserialize("Expression: |-\n  =Set(Status, \"✅ Complete\")", "Set(Status, \"✅ Complete\")");
        VerifyDeserialize("Expression: |-\n  =Text(\"温度: \" & Temp & \"℃\")", "Text(\"温度: \" & Temp & \"℃\")");

        // Test complex Unicode strings
        VerifyDeserialize("Expression: |-\n  =Set(变量_测试, \"龭唉𫓧G㐁A𫟦D𠳐ⅷ𫇭C丂\")", "Set(变量_测试, \"龭唉𫓧G㐁A𫟦D𠳐ⅷ𫇭C丂\")");
    }

    private void VerifySerialize(string? pfxScript, string expectedExpressionYaml)
    {
        var expression = pfxScript is null ? null : new PFxExpressionYaml(pfxScript);
        var testObject = new NamedPFxExpressionYaml(expression);
        var expectedYaml = expectedExpressionYaml is null ? "Expression:" : $"Expression: {expectedExpressionYaml}\n";
        var actualYaml = SerializeViaYamlDotNet(testObject);
        actualYaml.Should().Be(expectedYaml);
    }

    [TestMethod]
    public void SerializeAsPropertyBeingNull()
    {
        SerializeViaYamlDotNet(new NamedPFxExpressionYaml { Expression = null })
            .Should().Be("{}" + DefaultOptions.NewLine);
    }

    public record NamedPFxExpressionYaml
    {
        public NamedPFxExpressionYaml()
        {
        }

        public NamedPFxExpressionYaml(PFxExpressionYaml? expression)
        {
            Expression = expression;
        }

        public PFxExpressionYaml? Expression { get; init; }
    }
}
