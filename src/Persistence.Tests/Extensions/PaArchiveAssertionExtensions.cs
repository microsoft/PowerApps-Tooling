// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using FluentAssertions.Specialized;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

namespace Persistence.Tests.Extensions;

[DebuggerNonUserCode]
public static class PaArchiveAssertionExtensions
{
    public static PaArchiveAssertions Should(this PaArchive? value)
    {
        return new(value);
    }
}

[DebuggerNonUserCode]
public class PaArchiveAssertions(PaArchive? value)
    : ObjectAssertions<PaArchive?, PaArchiveAssertions>(value)
{
    public AndConstraint<PaArchiveAssertions> ContainEntry(string fullPath, string? because = null, params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject != null && Subject.ContainsEntry(fullPath))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:archive} to contain entry with path {0}{reason}, but was not found.",
                fullPath);

        return new(this);
    }

    public AndConstraint<PaArchiveAssertions> NotContainEntry(string fullPath, string? because = null, params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject != null && !Subject.ContainsEntry(fullPath))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:archive} to NOT contain entry with path {0}{reason}, but it was found.",
                fullPath);

        return new(this);
    }

    public AndConstraint<PaArchiveAssertions> HaveCountEntriesInDirectory(string directoryPath, int expectedCount, string? because = null, params object[] becauseArgs)
    {
        return HaveCountEntriesInDirectory(directoryPath, expectedCount, recursive: false, because, becauseArgs);
    }

    public AndConstraint<PaArchiveAssertions> HaveCountEntriesInDirectoryRecursive(string directoryPath, int expectedCount, string? because = null, params object[] becauseArgs)
    {
        return HaveCountEntriesInDirectory(directoryPath, expectedCount, recursive: true, because, becauseArgs);
    }

    public AndConstraint<PaArchiveAssertions> HaveCountEntriesInDirectory(string directoryPath, int expectedCount, bool recursive, string? because = null, params object[] becauseArgs)
    {
        var actualCount = Subject?.GetEntriesInDirectory(directoryPath, recursive: recursive).Count();
        Execute.Assertion
            .ForCondition(actualCount == expectedCount)
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:archive} to contain {0} entries in directory {1}{2}{reason}, but {3} were found.",
                expectedCount,
                directoryPath,
                recursive ? " recursive" : string.Empty,
                actualCount);
        return new(this);
    }

    public AndConstraint<PaArchiveAssertions> NotContainAnyEntriesInDirectory(string directoryPath, string? because = null, params object[] becauseArgs)
    {
        return NotContainAnyEntriesInDirectory(directoryPath, recursive: false, because, becauseArgs);
    }

    public AndConstraint<PaArchiveAssertions> NotContainAnyEntriesInDirectoryRecursive(string directoryPath, string? because = null, params object[] becauseArgs)
    {
        return NotContainAnyEntriesInDirectory(directoryPath, recursive: true, because, becauseArgs);
    }

    public AndConstraint<PaArchiveAssertions> NotContainAnyEntriesInDirectory(string directoryPath, bool recursive, string? because = null, params object[] becauseArgs)
    {
        var actualCount = Subject?.GetEntriesInDirectory(directoryPath, recursive: recursive).Count();
        Execute.Assertion
            .ForCondition(actualCount == 0)
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:archive} to NOT contain any entries in directory {0}{1}{reason}, but {2} were found.",
                directoryPath,
                recursive ? " recursive" : string.Empty,
                actualCount);
        return new(this);
    }
}
