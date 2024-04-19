// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions.Execution;
using FluentAssertions.Specialized;
using Microsoft.PowerPlatform.PowerApps.Persistence;

namespace Persistence.Tests.Extensions;

public static class PersistenceExceptionAssertionsExtensions
{
    public static ExceptionAssertions<TException> WithErrorCode<TException>(
        this ExceptionAssertions<TException> assertion,
        PersistenceErrorCode errorCode,
        string? because = null,
        params object[] becauseArgs)
        where TException : PersistenceException
    {
        _ = assertion ?? throw new ArgumentNullException(nameof(assertion));

        using var scope = new AssertionScope(context: $"exception of type {assertion.Which.GetType().Name}");
        var actualValue = assertion.Which.ErrorCode;
        Execute.Assertion
            .ForCondition(actualValue == errorCode)
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:exception} to have " + nameof(assertion.Which.ErrorCode) + " to be {0}{reason}, but found {1}.",
                errorCode, actualValue);

        return assertion;
    }

    public static ExceptionAssertions<TException> WithReason<TException>(
        this ExceptionAssertions<TException> assertion,
        string reason,
        string? because = null,
        params object[] becauseArgs)
        where TException : PersistenceException
    {
        _ = assertion ?? throw new ArgumentNullException(nameof(assertion));

        using var scope = new AssertionScope(context: $"exception of type {assertion.Which.GetType().Name}");
        var actualValue = assertion.Which.Reason;
        Execute.Assertion
            .ForCondition(actualValue == reason)
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:exception} to have " + nameof(assertion.Which.Reason) + " to be {0}{reason}, but found {1}.",
                reason, actualValue);

        return assertion;
    }
}
