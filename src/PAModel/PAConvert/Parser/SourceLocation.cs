// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR;

/// <summary>
/// Indices into file are 1-based.
/// </summary>
internal readonly record struct SourceLocation(int StartLine, int StartChar, int EndLine, int EndChar, string FileName)
{
    public static SourceLocation FromFile(string filename)
    {
        return new(0, 0, 0, 0, filename);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        WriteTo(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Writes this location to the given StringBuilder in the format "filename(startLine,startChar,endLine,endChar)".
    /// If the filename is empty, it is omitted.
    /// If the start and end positions are all zeros, the end position is omitted.
    /// </summary>
    internal void WriteTo(StringBuilder sb)
    {
        // Format using VS error format
        // 1>src\PAModel\PAConvert\Error.cs(42,11,42,11): error CS1002: ; expected
        if (FileName is not null)
        {
            sb.Append(FileName);
        }

        if (StartLine != default || StartChar != default || EndLine != default || EndChar != default)
        {
            sb.Append('(');
            sb.Append(StartLine);
            sb.Append(',');
            sb.Append(StartChar);
            sb.Append(',');
            sb.Append(EndLine);
            sb.Append(',');
            sb.Append(EndChar);
            sb.Append(')');
        }
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
}
