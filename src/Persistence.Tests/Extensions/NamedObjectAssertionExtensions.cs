// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

namespace Persistence.Tests.Extensions;

[DebuggerNonUserCode]
public static class NamedObjectAssertionExtensions
{
    public static NamedObjectAssertions<TValue> Should<TValue>(this NamedObject<TValue>? actualValue)
        where TValue : notnull
    {
        return new(actualValue);
    }
}

/// <summary>
/// Contains a number of methods to assert that a <see cref="NamedObjectMapping{TValue}"/> is in the expected state.
/// </summary>
[DebuggerNonUserCode]
public class NamedObjectAssertions<TValue>(NamedObject<TValue>? actualValue)
    : NamedObjectAssertions<NamedObject<TValue>?, string, TValue, NamedObjectAssertions<TValue>>(actualValue)
    where TValue : notnull;

/// <summary>
/// Contains a number of methods to assert that a <see cref="NamedObjectMapping{TValue}"/> is in the expected state.
/// </summary>
[DebuggerNonUserCode]
public class NamedObjectAssertions<TNamedObject, TName, TValue, TAssertions>(TNamedObject actualValue)
    : ReferenceTypeAssertions<TNamedObject, TAssertions>(actualValue)
    where TNamedObject : INamedObject<TName, TValue>?
    where TName : notnull
    where TValue : notnull
    where TAssertions : NamedObjectAssertions<TNamedObject, TName, TValue, TAssertions>
{
    protected override string Identifier => "NamedObject";

    public AndConstraint<TAssertions> HaveValueEqual(TValue expected, string because = "", params object[] becauseArgs)
    {
        _ = expected ?? throw new ArgumentNullException(nameof(expected));

        Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:NamedObject} to not be <null>{reason}.")
            .Then
            .ForCondition(Subject!.Value.Equals(expected))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:NamedObject} with name {0} to have a value equal to {1}{reason}, but is {2}.",
                Subject!.Name, expected, Subject!.Value);

        return new((TAssertions)this);
    }

    public AndConstraint<TAssertions> HaveStartEqual(int expectedLine, int expectedColumn, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:NamedObject} to not be <null>{reason}.")
            .Then
            .ForCondition(Subject!.Start is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:NamedObject} with name {0} to have a value for Start{reason}, but is <null>.", Subject!.Name)
            .Then
            .ForCondition(Subject!.Start?.Line == expectedLine && Subject!.Start?.Column == expectedColumn)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:NamedObject} with name {0} to have a Start of Line: {1}, Col: {2}{reason}, but is {3}.",
                Subject!.Name, expectedLine, expectedColumn, Subject!.Start);

        return new((TAssertions)this);
    }
}
