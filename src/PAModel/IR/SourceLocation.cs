using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR
{
    internal struct SourceLocation
    {
        public readonly int StartLine;
        public readonly int StartChar;
        public readonly int EndLine;
        public readonly int EndChar;
        public readonly string FileName;

        public SourceLocation(int startLine, int startChar, int endLine, int endChar, string fileName)
        {
            StartLine = startLine;
            StartChar = startChar;
            EndLine = endLine;
            EndChar = endChar;
            FileName = fileName;
        }
    }
}
