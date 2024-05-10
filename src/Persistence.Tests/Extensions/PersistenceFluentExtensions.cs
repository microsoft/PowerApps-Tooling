// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using YamlDotNet.RepresentationModel;

namespace Persistence.Tests.Extensions;

/// <summary>
/// Extensions for tests.
/// </summary>
internal static class PersistenceFluentExtensions
{
    /// <summary>
    /// Asserts that the value is not null and gives the nullable assertion.
    /// Will result in a FluentAssertionException if the value is null.
    /// </summary>
    public static void ShouldNotBeNull<T>([NotNull] this T? value)
        where T : notnull
    {
        if (value is null)
        {
            value.Should().NotBeNull();
            throw new InvalidOperationException("Should().NotBeNull() should have thrown.");
        }
    }

    public static AndConstraint<TAssertions> NotDefineMember<TSubject, TAssertions>(this ObjectAssertions<TSubject, TAssertions> assertions, string memberName, string because = "", params object[] becauseArgs)
        where TAssertions : ObjectAssertions<TSubject, TAssertions>
        where TSubject : class
    {
        _ = assertions ?? throw new ArgumentNullException(nameof(assertions));

        assertions.Subject.ShouldNotBeNull();
        if (assertions.Subject is not null)
        {
            var subjectType = assertions.Subject.GetType();
            var matchingMembers = subjectType.GetMembers().Where(m => m.Name == memberName).ToArray();

            Execute.Assertion
                .ForCondition(matchingMembers!.Length == 0)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:Type} with Type name {0} to not have a member with name {1}{reason}, but does.",
                    subjectType.Name, memberName);
        }

        return new AndConstraint<TAssertions>((TAssertions)assertions);
    }

    public static AndConstraint<TAssertions> BeYamlEquivalentTo<TAssertions>(this StringAssertions<TAssertions> assertions, string expectedYaml, string because = "", params object[] becauseArgs)
        where TAssertions : StringAssertions<TAssertions>
    {
        _ = assertions ?? throw new ArgumentNullException(nameof(assertions));
        _ = expectedYaml ?? throw new ArgumentNullException(nameof(expectedYaml));

        assertions.Subject.ShouldNotBeNull();

        var actualYamlStream = new YamlStream();
        using (var actualTextReader = new StringReader(assertions.Subject))
        {
            actualYamlStream.Load(actualTextReader);
        }

        var expectedYamlStream = new YamlStream();
        using (var expectedTextReader = new StringReader(expectedYaml))
        {
            expectedYamlStream.Load(expectedTextReader);
        }

        var context = nameof(actualYamlStream);
        using (var scope = new AssertionScope(context))
        {
            CompareYamlStreams(actualYamlStream, expectedYamlStream, context);
        }

        //actualYamlStream.Should().BeEquivalentTo(expectedYamlStream, because, becauseArgs);

        return new AndConstraint<TAssertions>((TAssertions)assertions);
    }

    private static void CompareYamlStreams(YamlStream actualYamlStream, YamlStream expectedYamlStream, string? contextBase = null)
    {
        contextBase ??= nameof(actualYamlStream);

        foreach (var (docIdx, actualDoc, expectedDoc) in actualYamlStream.Documents.ZipWithIndex(expectedYamlStream.Documents))
        {
            var context = $"{contextBase}.{nameof(actualYamlStream.Documents)}[{docIdx}]";
            CompareYamlTree(actualDoc, expectedDoc, context);
        }

        // Zip will only validate items that are in each list
        actualYamlStream.Documents.Should().HaveSameCount(expectedYamlStream.Documents);
    }

    private static void CompareYamlTree(YamlDocument actualDoc, YamlDocument expectedDoc, string docContext)
    {
        docContext ??= nameof(actualDoc);

        using (var scope = new AssertionScope(docContext))
        {
            var nodePath = string.Empty; // document root
            CompareYamlTree(actualDoc.RootNode, expectedDoc.RootNode, nodePath);
        }
    }

    private static void CompareYamlTree(YamlNode actualNode, YamlNode expectedNode, string nodePath)
    {
        const string BecauseFormat_PropertyName_NodePath_NodeStart = "(Node property: {0}, node path: '{1}', node location: {2})";

        _ = actualNode ?? throw new ArgumentNullException(nameof(actualNode));
        _ = expectedNode ?? throw new ArgumentNullException(nameof(expectedNode));
        _ = nodePath ?? throw new ArgumentNullException(nameof(nodePath));

        actualNode.NodeType.Should().Be(expectedNode.NodeType, BecauseFormat_PropertyName_NodePath_NodeStart, nameof(actualNode.NodeType), nodePath, actualNode.Start);

        // TODO: add option of whether to compare the Style properties.

        // Only compare the properties relevant to yaml equivalence
        if (actualNode.NodeType == expectedNode.NodeType)
        {
            if (actualNode.NodeType == YamlNodeType.Scalar)
            {
                var actualScalar = (YamlScalarNode)actualNode;
                var expectedScalar = (YamlScalarNode)expectedNode;

                // Per YamlScalarNode's Equals implementation, the value and tag are the only properties that matter
                actualScalar.Tag.Should().Be(expectedScalar.Tag, BecauseFormat_PropertyName_NodePath_NodeStart, nameof(actualNode.Tag), nodePath, actualNode.Start);
                actualScalar.Value.Should().Be(expectedScalar.Value, BecauseFormat_PropertyName_NodePath_NodeStart, nameof(actualScalar.Value), nodePath, actualNode.Start);
            }
            else if (actualNode.NodeType == YamlNodeType.Sequence)
            {
                var actualSequence = (YamlSequenceNode)actualNode;
                var expectedSequence = (YamlSequenceNode)expectedNode;

                actualSequence.Tag.Should().Be(expectedSequence.Tag, BecauseFormat_PropertyName_NodePath_NodeStart, nameof(actualNode.Tag), nodePath, actualNode.Start);

                foreach (var (childIdx, actualChild, expectedChild) in actualSequence.Children.ZipWithIndex(expectedSequence.Children))
                {
                    var childNodePath = $"{nodePath}[{childIdx}]";
                    CompareYamlTree(actualChild, expectedChild, childNodePath);
                }

                // Zip will only validate items that are in each list
                actualSequence.Children.Should().HaveSameCount(expectedSequence.Children, BecauseFormat_PropertyName_NodePath_NodeStart, nameof(actualSequence.Children), nodePath, actualNode.Start);
            }
            else if (actualNode.NodeType == YamlNodeType.Mapping)
            {
                var actualMapping = (YamlMappingNode)actualNode;
                var expectedMapping = (YamlMappingNode)expectedNode;

                actualMapping.Tag.Should().Be(expectedMapping.Tag, BecauseFormat_PropertyName_NodePath_NodeStart, nameof(actualNode.Tag), nodePath, actualNode.Start);

                // Report mising/extra keys first
                var actualMappingKeys = actualMapping.Children.Keys.Select(k => k.ToString()).ToArray();
                var expectedMappingKeys = expectedMapping.Children.Keys.Select(k => k.ToString()).ToArray();
                actualMappingKeys.Should().BeEquivalentTo(expectedMappingKeys, BecauseFormat_PropertyName_NodePath_NodeStart, nameof(actualMapping.Children), nodePath, actualNode.Start);

                // Then dig down into each key expected
                foreach (var (expectedKey, expectedValue) in expectedMapping.Children)
                {
                    // Note: technically, YAML mapping keys can be any yaml node type (e.g. scalar, sequence, mapping, etc.)
                    // For now, we'll assume that the keys are always scalars and verify here:
                    if (expectedKey.NodeType != YamlNodeType.Scalar)
                    {
                        throw new NotSupportedException($"Non-scalar key found in expected mapping at path '{nodePath}'.");
                    }

                    var valueNodePath = $"{nodePath}{((YamlScalarNode)expectedKey).ToBreadcrumbPathSegment()}";
                    if (actualMapping.Children.TryGetValue(expectedKey, out var actualValue))
                    {
                        CompareYamlTree(actualValue, expectedValue, valueNodePath);
                    }
                }
            }
            else if (actualNode.NodeType == YamlNodeType.Alias)
            {
                // Note: YamlAliasNode is an internal class
                actualNode.Anchor.Should().Be(expectedNode.Anchor, BecauseFormat_PropertyName_NodePath_NodeStart, nameof(actualNode.Anchor), nodePath, actualNode.Start);
            }
        }
    }

    private static readonly Regex RequiresEscapingRegex = new(@"[^a-zA-Z0-1_-]", RegexOptions.Compiled);

    private static string ToBreadcrumbPathSegment(this YamlScalarNode node)
    {
        if (node.Value == null)
        {
            return "[~]";
        }

        if (RequiresEscapingRegex.IsMatch(node.Value))
        {
            return $"[\"{node.Value}\"]";
        }

        return $".{node.Value}";
    }

    //private static void CompareYamlNodesNonRecursive(IEnumerable<YamlNode> actualNodes, IEnumerable<YamlNode> expectedNodes, string? contextBase = null)
    //{
    //    contextBase ??= nameof(actualNodes);

    //    actualNodes.Should().HaveSameCount(expectedNodes);

    //    foreach (var (nodeIdx, actualNode, expectedNode) in actualNodes.ZipWithIndex(expectedNodes))
    //    {
    //        var context = $"{contextBase}[{nodeIdx}]";
    //        using (var scope = new AssertionScope(context))
    //            CompareYamlNodeNonRecursive(actualNode, expectedNode, context);
    //    }
    //}

    //private static void CompareYamlNodeNonRecursive(YamlNode actualNode, YamlNode expectedNode, string? contextBase = null)
    //{
    //    contextBase ??= nameof(actualNode);

    //    actualNode.NodeType.Should().Be(expectedNode.NodeType, contextBase);

    //    // TODO: add option of whether to compare the Style properties.

    //    // Only compare the properties relevant to yaml equivalence
    //    if (actualNode.NodeType == expectedNode.NodeType)
    //    {
    //        if (actualNode.NodeType == YamlNodeType.Scalar)
    //        {
    //            var actualScalar = (YamlScalarNode)actualNode;
    //            var expectedScalar = (YamlScalarNode)expectedNode;

    //            // Per YamlScalarNode's Equals implementation, the value and tag are the only properties that matter
    //            actualScalar.Tag.Should().Be(expectedScalar.Tag, contextBase);
    //            actualScalar.Value.Should().Be(expectedScalar.Value, contextBase);
    //        }
    //        else if (actualNode.NodeType == YamlNodeType.Sequence)
    //        {
    //            var actualSequence = (YamlSequenceNode)actualNode;
    //            var expectedSequence = (YamlSequenceNode)expectedNode;

    //            actualSequence.Tag.Should().Be(expectedSequence.Tag, contextBase);
    //            actualSequence.Children.Should().HaveSameCount(expectedSequence.Children, contextBase);
    //        }
    //        else if (actualNode.NodeType == YamlNodeType.Mapping)
    //        {
    //            var actualMapping = (YamlMappingNode)actualNode;
    //            var expectedMapping = (YamlMappingNode)expectedNode;

    //            actualMapping.Tag.Should().Be(expectedMapping.Tag, contextBase);
    //            actualMapping.Children.Should().HaveSameCount(expectedMapping.Children, contextBase);
    //        }
    //        else if (actualNode.NodeType == YamlNodeType.Alias)
    //        {
    //            // Note: YamlAliasNode is an internal class
    //            actualNode.Anchor.Should().Be(expectedNode.Anchor, contextBase);
    //        }
    //    }
    //}

    public static IEnumerable<(int Index, T1 Left, T2 Right)> ZipWithIndex<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second)
    {
        _ = first ?? throw new ArgumentNullException(nameof(first));
        _ = second ?? throw new ArgumentNullException(nameof(second));

        return first.Zip(second)
            .Select((item, index) => (index, item.First, item.Second));
    }

    public static void WriteTextWithLineNumbers(this TestContext testContext, string text, string? headerLine = null)
    {
        _ = testContext ?? throw new ArgumentNullException(nameof(testContext));

        var sb = new StringBuilder();
        if (headerLine is not null)
        {
            sb.AppendLine(headerLine);
        }

        StringReader reader = new(text);
        var lineNumber = 1;
        var line = reader.ReadLine();
        while (line is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"{lineNumber,3}: {line}");
            line = reader.ReadLine();
            lineNumber++;
        }

        // We write all at one time to avoid any interleaving of lines from multiple threads
        testContext.WriteLine(sb.ToString());
    }
}
