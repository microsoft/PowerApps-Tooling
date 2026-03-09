// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Persistence.Tests;

/// <summary>
/// MSTest assembly-level initializer. Runs once before any tests in this assembly.
/// </summary>
[TestClass]
public static class TestAssemblyInitializer
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext _)
    {
        // Replace the default TraceListener (which shows a dialog or aborts) with one that
        // throws an exception, so that Debug.Assert failures are surfaced as test failures.
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new ThrowingTraceListener());
    }

    /// <summary>
    /// A <see cref="TraceListener"/> that converts <see cref="Debug.Assert(bool)"/> failures into
    /// <see cref="InvalidOperationException"/>s, making them visible as test failures.
    /// </summary>
    private sealed class ThrowingTraceListener : TraceListener
    {
        public override void Write(string? message) { }
        public override void WriteLine(string? message) { }

        public override void Fail(string? message, string? detailMessage)
        {
            var fullMessage = string.IsNullOrEmpty(detailMessage)
                ? $"Debug.Assert failed: {message}"
                : $"Debug.Assert failed: {message}{Environment.NewLine}{detailMessage}";
            throw new InvalidOperationException(fullMessage);
        }
    }
}
