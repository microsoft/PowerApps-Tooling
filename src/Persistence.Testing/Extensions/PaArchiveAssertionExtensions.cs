// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using FluentAssertions.Specialized;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Testing.Extensions;

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
    public AndWhichConstraint<PaArchiveAssertions, PaArchiveEntry> HaveEntry(string fullPath, string? because = null, params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject != null && Subject.ContainsEntry(fullPath))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:archive} to have an entry with path {0}{reason}, but was not found.",
                fullPath);

        return new(this, Subject?.GetEntryOrDefault(fullPath)!);
    }

    public AndConstraint<PaArchiveAssertions> NotHaveEntry(string fullPath, string? because = null, params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject != null && !Subject.ContainsEntry(fullPath))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:archive} to NOT have an entry with path {0}{reason}, but it was found.",
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
                "Expected {context:archive} to have {0} entries in directory {1}{2}{reason}, but {3} were found.",
                expectedCount,
                directoryPath,
                recursive ? " recursive" : string.Empty,
                actualCount);
        return new(this);
    }

    public AndConstraint<PaArchiveAssertions> NotHaveAnyEntriesInDirectory(string directoryPath, string? because = null, params object[] becauseArgs)
    {
        return NotHaveAnyEntriesInDirectory(directoryPath, recursive: false, because, becauseArgs);
    }

    public AndConstraint<PaArchiveAssertions> NotHaveAnyEntriesInDirectoryRecursive(string directoryPath, string? because = null, params object[] becauseArgs)
    {
        return NotHaveAnyEntriesInDirectory(directoryPath, recursive: true, because, becauseArgs);
    }

    public AndConstraint<PaArchiveAssertions> NotHaveAnyEntriesInDirectory(string directoryPath, bool recursive, string? because = null, params object[] becauseArgs)
    {
        var actualCount = Subject?.GetEntriesInDirectory(directoryPath, recursive: recursive).Count();
        Execute.Assertion
            .ForCondition(actualCount == 0)
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:archive} to NOT have any entries in directory {0}{1}{reason}, but {2} were found.",
                directoryPath,
                recursive ? " recursive" : string.Empty,
                actualCount);
        return new(this);
    }
}
