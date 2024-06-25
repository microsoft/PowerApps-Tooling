// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// Source location
/// </summary>
[DebuggerDisplay("l:{Line}, c:{Column}, f:{FilePath}")]
public record SourceLocation
{
    /// <summary>
    /// File path
    /// </summary>
    public string? FilePath { get; init; }
    public int? Line { get; init; }
    public int? Column { get; init; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public SourceLocation()
    {
    }

    /// <summary>
    /// Parameterized constructor
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="line"></param>
    /// <param name="column"></param>
    public SourceLocation(string? filePath, int? line, int? column)
    {
        FilePath = filePath;

        if (line != null && line < 0)
            throw new ArgumentOutOfRangeException(nameof(line));
        Line = line;

        if (column != null && column < 0)
            throw new ArgumentOutOfRangeException(nameof(column));
        Column = column;
    }

    /// <summary>
    /// Copy constructor 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public SourceLocation(SourceLocation sourceLocation)
    {
        FilePath = sourceLocation.FilePath;
        Line = sourceLocation.Line;
        Column = sourceLocation.Column;
    }
}
