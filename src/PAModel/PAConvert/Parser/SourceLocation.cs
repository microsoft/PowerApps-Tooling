// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR;

internal readonly struct SourceLocation(int startLine, int startChar, int endLine, int endChar, string fileName)
{
    public readonly int StartLine = startLine;
    public readonly int StartChar = startChar;
    public readonly int EndLine = endLine;
    public readonly int EndChar = endChar;
    public readonly string FileName = fileName;

    // Indices into file are 1-based.

    public static SourceLocation FromFile(string filename)
    {
        return new SourceLocation(0, 0, 0, 0, filename);
    }

    public override string ToString()
    {
        return $"{FileName}:{StartLine},{StartChar}-{EndLine},{EndChar}";
    }

    public static SourceLocation FromChildren(List<SourceLocation> locations)
    {
        SourceLocation minLoc = locations.First(), maxLoc = locations.First();

        foreach (var loc in locations)
        {
            if (loc.StartLine < minLoc.StartLine ||
                (loc.StartLine == minLoc.StartLine && loc.StartChar < minLoc.StartChar))
            {
                minLoc = loc;
            }
            if (loc.EndLine > maxLoc.EndLine ||
                (loc.EndLine == minLoc.EndLine && loc.EndChar > minLoc.EndChar))
            {
                maxLoc = loc;
            }
        }

        return new SourceLocation(minLoc.StartLine, minLoc.StartChar, maxLoc.EndLine, maxLoc.EndChar, maxLoc.FileName);
    }

    public override bool Equals(object obj)
    {
        return obj is SourceLocation other &&
            other.FileName == FileName &&
            other.StartChar == StartChar &&
            other.StartLine == StartLine &&
            other.EndChar == EndChar &&
            other.EndLine == EndLine;
    }

    public override int GetHashCode()
    {
        return (FileName, StartChar, EndChar, StartLine, EndLine).GetHashCode();
    }
}
