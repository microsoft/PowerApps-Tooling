// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions.Collections;
using FluentAssertions.Execution;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

namespace Persistence.Tests.Extensions;

[DebuggerNonUserCode]
public static class NamedObjectCollectionAssertionExtensions
{
    public static NamedObjectCollectionAssertions<TValue> Should<TValue>(this IReadOnlyNamedObjectCollection<TValue>? actualValue)
        where TValue : notnull
    {
        return new(actualValue);
    }
}

/// <summary>
/// Contains a number of methods to assert that a <see cref="NamedObjectMapping{TValue}"/> is in the expected state.
/// </summary>
[DebuggerNonUserCode]
public class NamedObjectCollectionAssertions<TValue>
    : NamedObjectCollectionAssertions<IReadOnlyNamedObjectCollection<TValue>?, TValue, NamedObjectCollectionAssertions<TValue>>
    where TValue : notnull
{
    public NamedObjectCollectionAssertions(IReadOnlyNamedObjectCollection<TValue>? actualValue)
        : base(actualValue)
    {
    }
}

/// <summary>
/// Contains a number of methods to assert that a <see cref="NamedObjectMapping{TValue}"/> is in the expected state.
/// </summary>
[DebuggerNonUserCode]
public class NamedObjectCollectionAssertions<TCollection, TValue, TAssertions>
    : GenericCollectionAssertions<TCollection, NamedObject<TValue>, TAssertions>
    where TCollection : IReadOnlyNamedObjectCollection<TValue>?
    where TValue : notnull
    where TAssertions : NamedObjectCollectionAssertions<TCollection, TValue, TAssertions>
{
    public NamedObjectCollectionAssertions(TCollection actualValue)
        : base(actualValue)
    {
    }

    public AndConstraint<TAssertions> ContainNames(params string[] expected)
    {
        return ContainNames(expected, string.Empty);
    }

    public AndConstraint<TAssertions> ContainNames(IEnumerable<string> expected, string because = "", params object[] becauseArgs)
    {
        _ = expected ?? throw new ArgumentNullException(nameof(expected));
        var expectedNames = expected as ICollection<string> ?? expected.ToList();

        Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:NamedObject collection} to contain names {0}{reason}, but found <null>.", expected);

        if (Subject is not null)
        {
            var missingKeys = expectedNames.Where(name => !Subject.Contains(name));
            if (missingKeys.Any())
            {
                if (expectedNames.Count > 1)
                {
                    Execute.Assertion
                        .BecauseOf(because, becauseArgs)
                        .FailWith("Expected {context:NamedObject collection} with names {0} to contain names {1}{reason}, but could not find {2}.",
                            Subject.Names, expected, missingKeys);
                }
                else
                {
                    Execute.Assertion
                        .BecauseOf(because, becauseArgs)
                        .FailWith("Expected {context:NamedObject collection} with names {0} to contain name {1}{reason}.",
                            Subject.Names, expected.First());
                }
            }
        }

        return new AndConstraint<TAssertions>((TAssertions)this);
    }

    public WhoseNamedObjectConstraint<TCollection, TValue, TAssertions> ContainName(string expected, string because = "", params object[] becauseArgs)
    {
        _ = expected ?? throw new ArgumentNullException(nameof(expected));

        var andConstraint = ContainNames(new[] { expected }, because, becauseArgs);
        _ = Subject!.TryGetNamedObject(expected, out var namedObject);

        return new WhoseNamedObjectConstraint<TCollection, TValue, TAssertions>(andConstraint.And, namedObject!);
    }
}

[DebuggerNonUserCode]
public class WhoseNamedObjectConstraint<TCollection, TValue, TAssertions> : AndConstraint<TAssertions>
    where TCollection : IReadOnlyNamedObjectCollection<TValue>?
    where TValue : notnull
    where TAssertions : NamedObjectCollectionAssertions<TCollection, TValue, TAssertions>
{
    public WhoseNamedObjectConstraint(TAssertions parentConstraint, NamedObject<TValue> namedObject) : base(parentConstraint)
    {
        WhoseNamedObject = namedObject;
    }

    public NamedObject<TValue> WhoseNamedObject { get; }
    public TValue WhoseValue => WhoseNamedObject.Value;
}
