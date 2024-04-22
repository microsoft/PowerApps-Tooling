// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
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
        string? wildcardPattern,
        string? because = null,
        params object[] becauseArgs)
        where TException : PersistenceException
    {
        _ = assertion ?? throw new ArgumentNullException(nameof(assertion));

        using var scope = new AssertionScope(context: $"exception of type {assertion.Which.GetType().Name}");
        var actualValue = assertion.Which.Reason;

        if (wildcardPattern == null)
        {
            Execute.Assertion
                .ForCondition(actualValue == null)
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected {context:exception} to have " + nameof(assertion.Which.Reason) + " to be <null>{reason}, but found {0}.",
                    actualValue);
        }
        else
        {
            var regexPattern = CreateRegexFromWildcardPattern(wildcardPattern);
            Execute.Assertion
                .ForCondition(actualValue != null && regexPattern.IsMatch(actualValue))
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected {context:exception} to have " + nameof(assertion.Which.Reason) + " to be {0}{reason}, but found {1}.",
                    wildcardPattern, actualValue);
        }

        return assertion;
    }

    /// <summary>
    /// Converts a string that represents a FluentAssertion wildcard pattern into it's equivalent regular expression.
    /// </summary>
    /// <param name="wildcardPattern">A sting that supports '*' and '?' chars as wildcards.</param>
    private static Regex CreateRegexFromWildcardPattern(string wildcardPattern, bool ignoreCase = false)
    {
        var regexPattern = Regex.Escape(wildcardPattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal);
        return new($"^{regexPattern}$", options: (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Singleline);
    }
}
