// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using YamlDotNet.Serialization;

namespace Persistence.Tests.PaYaml.Serialization;

[TestClass]
public class PFxExpressionYamlConverterTests : TestBase
{
    private static readonly PFxExpressionYamlFormattingOptions FailSafeOptions = new()
    {
        ForceLiteralBlockIfContainsAny = null //new[] { "\"" }
    };

    [TestMethod]
    public void ReadYamlWithFailSafeOptions()
    {
        var deserializer = new DeserializerBuilder()
            .WithTypeConverter(new PFxExpressionYamlConverter(FailSafeOptions))
            .Build();

        void VerifyDeserialize(string yaml, string? expectedScript)
        {
            var testObject = deserializer.Deserialize<NamedPFxExpressionYaml>(yaml);
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

    [TestMethod]
    public void WriteYamlWithFailSafeOptions()
    {
        var serializer = new SerializerBuilder()
            .WithNewLine("\n") // Ensure tests are consistent across platforms
            .WithTypeConverter(new PFxExpressionYamlConverter(FailSafeOptions))
            .Build();

        void VerifySerialize(string? pfxScript, string expectedExpressionYaml)
        {
            var expression = pfxScript is null ? null : new PFxExpressionYaml(pfxScript);
            var testObject = new NamedPFxExpressionYaml(expression);
            var expectedYaml = expectedExpressionYaml is null ? "Expression:" : $"Expression: {expectedExpressionYaml}\n";
            var actualYaml = serializer.Serialize(testObject);
            actualYaml.Should().Be(expectedYaml);
        }

        // Only the canonical null literal will be written:
        VerifySerialize(null, "");

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
