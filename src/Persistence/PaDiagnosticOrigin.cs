// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

public record PaDiagnosticOrigin
{
    public PaDiagnosticOrigin(string? toolOrFilePath)
    {
        ToolOrFilePath = toolOrFilePath;
    }

    /// <summary>
    /// The invariant tool name, file name, or file path where the diagnostic message occurred.
    /// </summary>
    public string? ToolOrFilePath { get; }

    public (long Line, long? Column)? Start { get; init; }
    public (long Line, long? Column)? End { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        AppendTo(sb, loggerSafeOnly: false);
        return sb.ToString();
    }

    internal void AppendTo(StringBuilder sb, bool loggerSafeOnly)
    {
        // PRIVACY: File paths are considered personal data even inside an msapp (e.g. screen names (considered personal data) are used for filenames in msapp)
        if (ToolOrFilePath != null && !loggerSafeOnly)
        {
            sb.Append(ToolOrFilePath);
        }

        if (Start != null)
        {
            var (startLine, startCol) = Start.Value;
            var (endLine, endCol) = (End ?? Start).Value;

            // Normalize that it's an error to specify the endCol without specifying the startCol
            if (startCol == null)
            {
                endCol = null;
            }

            if (startLine == endLine)
            {
                if (startCol == null)
                {
                    // (line)
                    sb.Append(CultureInfo.InvariantCulture, $"({startLine})");
                }
                else
                {
                    if (endCol != null && endCol.Value != startCol.Value)
                    {
                        // (line,col-col)
                        sb.Append(CultureInfo.InvariantCulture, $"({startLine},{startCol.Value}-{endCol.Value})");
                    }
                    else
                    {
                        // (line,col)
                        sb.Append(CultureInfo.InvariantCulture, $"({startLine},{startCol.Value})");
                    }
                }
            }
            else // startLine != endLine
            {
                if (startCol != null)
                {
                    // (line,col,line,col)
                    // We default the endCol to 0 if missing as it is the safe ending spot on the end line.
                    sb.Append(CultureInfo.InvariantCulture, $"({startLine},{startCol.Value},{endLine},{endCol ?? 0})");
                }
                else
                {
                    // (line-line)
                    sb.Append(CultureInfo.InvariantCulture, $"({startLine}-{endLine})");
                }
            }
        }
    }
}
