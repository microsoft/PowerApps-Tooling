// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using FluentAssertions.Execution;

namespace Microsoft.PowerPlatform.TypedStrings;

[TestClass]
public class TypedStringTests
{
    [TestMethod]
    public void NonEmptyStringInvalidInputTests()
    {
        RunInvalidTypedStringInputTest<NonEmptyString>([
            null!,
            string.Empty,
            ]);
    }

    [TestMethod]
    public void NonEmptyStringValidInputTests()
    {
        RunValidTypedStringInputTest<NonEmptyString>([
            " ",
            "\t",
            " \t ",
            "x",
            "Fo0",
            " x ",
            ]);
    }

    [TestMethod]
    public void NonWhitespaceStringInvalidInputTests()
    {
        RunInvalidTypedStringInputTest<NonWhitespaceString>([
            null!,
            string.Empty,
            " ",
            "\t",
            " \t ",
            ]);
    }

    [TestMethod]
    public void NonWhitespaceStringValidInputTests()
    {
        RunValidTypedStringInputTest<NonWhitespaceString>([
            "x",
            "Fo0",
            " x ",
            ]);
    }

    [TestMethod]
    public void NonWhitespaceStringFluentAssertionTests()
    {
        var actual = new TestRecord()
        {
            ANonWhitespaceString = new("Foo"),
        };
        actual.Should().Be(new TestRecord { ANonWhitespaceString = new("Foo") }, "expected is of type TestRecord")
            .And.BeEquivalentTo(new TestRecord { ANonWhitespaceString = new("Foo") }, "expected is of type TestRecord");
        actual.Should().BeEquivalentTo(new { ANonWhitespaceString = new NonWhitespaceString("Foo") }, "expected is annonymous type with correct properties and property value types");

        // TODO: Uncomment when we auto-implement IConvertible for FluentAssertions to work:
        //actual.Should().BeEquivalentTo(new { ANonWhitespaceString = "Foo" },
        //    // In order to utilize the IConvertible implementation, we need to use WithAutoConversion.
        //    // But this allows us to construct an anonymous expecteation with strings, making it easier to write.
        //    config => config.WithAutoConversion(),
        //    "expected is anonymous type with properties where the value is a string, but should be able to be compared to the strong-typed string");
    }

    private sealed record TestRecord
    {
        public NonEmptyString? ANonEmptyString { get; init; }
        public ImmutableArray<NonEmptyString> NonEmptyStrings { get; init; }

        public NonWhitespaceString? ANonWhitespaceString { get; init; }
        public ImmutableArray<NonWhitespaceString> NonWhitespaceStrings { get; init; }
    }

    public static void RunInvalidTypedStringInputTest<TSelf>(string?[] invalidInputs)
        where TSelf : ITypedString<TSelf>
    {
        foreach (var input in invalidInputs)
        {
            using var _ = new AssertionScope(input is null ? "for input of null" : $"for input of '{input}'");

            TSelf.TryParse(input, provider: null, out var result).Should().BeFalse();
            result.Should().BeNull();

            // Check Parse after TryParse since it's expected to be implemented via TryParse.
            if (input is null)
            {
                CreateAction(() => TSelf.Parse(input!, provider: null)).Should().Throw<ArgumentNullException>();
            }
            else
            {
                CreateAction(() => TSelf.Parse(input, provider: null)).Should().Throw<FormatException>();
            }
        }
    }

    public static void RunValidTypedStringInputTest<TSelf>(string[] validInputs)
        where TSelf : ITypedString<TSelf>
    {
        foreach (var input in validInputs)
        {
            using var _ = new AssertionScope($"for input of '{input}'");

            TSelf.TryParse(input, provider: null, out var result1).Should().BeTrue();
            result1.Should().NotBeNull()
                .And.BeOfType<TSelf>()
                .Which.Value.Should().Be(input);

            // Check Parse after TryParse since it's expected to be implemented via TryParse.
            TSelf.Parse(input, provider: null).Should().NotBeNull()
                .And.BeOfType<TSelf>()
                .Which.Value.Should().Be(input);
        }
    }

    private static Action CreateAction(Action action) => action;
}
