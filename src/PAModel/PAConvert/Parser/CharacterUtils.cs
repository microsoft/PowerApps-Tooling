// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Parser;

internal static class CharacterUtils
{
    /// <summary>
    /// Bit masks of the UnicodeCategory enum. A couple extra values are defined
    /// for convenience for the C# lexical grammar.
    /// </summary>
    [Flags]
    public enum UniCatFlags : uint
    {
        // Letters
        LowercaseLetter = 1 << UnicodeCategory.LowercaseLetter, // Ll
        UppercaseLetter = 1 << UnicodeCategory.UppercaseLetter, // Lu
        TitlecaseLetter = 1 << UnicodeCategory.TitlecaseLetter, // Lt
        ModifierLetter = 1 << UnicodeCategory.ModifierLetter, // Lm
        OtherLetter = 1 << UnicodeCategory.OtherLetter, // Lo

        // Marks
        NonSpacingMark = 1 << UnicodeCategory.NonSpacingMark, // Mn
        SpacingCombiningMark = 1 << UnicodeCategory.SpacingCombiningMark, // Mc

        // Numbers
        DecimalDigitNumber = 1 << UnicodeCategory.DecimalDigitNumber, // Nd
        LetterNumber = 1 << UnicodeCategory.LetterNumber, // Nl (i.e. roman numeral one 0x2160)

        // Spaces
        SpaceSeparator = 1 << UnicodeCategory.SpaceSeparator, // Zs
        LineSeparator = 1 << UnicodeCategory.LineSeparator, // Zl
        ParagraphSeparator = 1 << UnicodeCategory.ParagraphSeparator, // Zp

        // Other
        Format = 1 << UnicodeCategory.Format, // Cf
        Control = 1 << UnicodeCategory.Control, // Cc
        OtherNotAssigned = 1 << UnicodeCategory.OtherNotAssigned, // Cn
        PrivateUse = 1 << UnicodeCategory.PrivateUse, // Co
        Surrogate = 1 << UnicodeCategory.Surrogate, // Cs

        // Punctuation
        ConnectorPunctuation = 1 << UnicodeCategory.ConnectorPunctuation, // Pc

        // Useful combinations.
        IdentStartChar = UppercaseLetter | LowercaseLetter | TitlecaseLetter |
          ModifierLetter | OtherLetter | LetterNumber,

        IdentPartChar = IdentStartChar | NonSpacingMark | SpacingCombiningMark |
          DecimalDigitNumber | ConnectorPunctuation | Format,
    }

    public static UniCatFlags GetUniCatFlags(char ch)
    {
        return (UniCatFlags)(1u << (int)CharUnicodeInfo.GetUnicodeCategory(ch));
    }

    // Returns true if the specified character is a valid simple identifier character.
    public static bool IsSimpleIdentCh(char ch)
    {
        if (ch >= 128)
            return (GetUniCatFlags(ch) & UniCatFlags.IdentPartChar) != 0;
        return ((uint)(ch - 'a') < 26) || ((uint)(ch - 'A') < 26) || ((uint)(ch - '0') <= 9) || (ch == '_');
    }

    // Returns true if the specified character is valid as the first character of an identifier.
    // If an identifier contains any other characters, it has to be surrounded by single quotation marks.
    public static bool IsIdentStart(char ch)
    {
        if (ch >= 128)
            return (GetUniCatFlags(ch) & UniCatFlags.IdentStartChar) != 0;
        return ((uint)(ch - 'a') < 26) || ((uint)(ch - 'A') < 26) || (ch == '_') || (ch == PAConstants.IdentifierDelimiter);
    }

    public static bool IsIdentDelimiter(char ch)
    {
        return ch == PAConstants.IdentifierDelimiter;
    }

    public static bool IsSpace(char ch)
    {
        if (ch >= 128)
            return (GetUniCatFlags(ch) & UniCatFlags.SpaceSeparator) != 0;

        switch (ch)
        {
            case ' ':
            case '\u0009':
            // line tabulation
            case '\u000B':
            // form feed
            case '\u000C':
                return true;
        }

        return false;
    }

    public static bool IsLineTerm(char ch)
    {
        switch (ch)
        {
            // line feed, unicode 0x000A
            case '\n':
            // carriage return, unicode 0x000D
            case '\r':
            // Unicode next line
            case '\u0085':
            // Unicode line separator
            case '\u2028':
            // Unicode paragraph separator
            case '\u2029':
                return true;
        }

        return false;
    }

    public static string UnEscapeName(string name, ErrorContainer errors)
    {
        if (IsIdentDelimiter(name[0]))
        {
            if (!IsIdentDelimiter(name[^1]))
            {
                // Must match
                errors.ParseError(default, $"Unmatched escape delimeter in {name}");
                throw new DocumentException();
            }
            var sb = new StringBuilder();
            var i = 1;
            while (i <= name.Length - 2)
            {
                var ch = name[i];
                if (ch == PAConstants.IdentifierDelimiter)
                {
                    i++;
                }
                sb.Append(ch);
                i++;
            }
            return sb.ToString();

        }
        else
        {
            return name;
        }
    }

    public static string EscapeName(string name)
    {
        var nameLen = name.Length;

        var fEscaping = !IsIdentStart(name[0]) || IsIdentDelimiter(name[0]);
        var fFirst = true;

        var sb = new StringBuilder(name.Length);
        for (var i = fEscaping ? 0 : 1; i < nameLen; i++)
        {
            var ch = name[i];
            fEscaping = fEscaping || !IsSimpleIdentCh(ch);

            if (!fEscaping)
                continue;

            if (fFirst)
            {
                sb.Append(PAConstants.IdentifierDelimiter);
                sb.Append(name, 0, i);
                fFirst = false;
            }

            if (ch == PAConstants.IdentifierDelimiter)
                sb.Append(ch);

            sb.Append(ch);
        }

        if (fEscaping)
        {
            sb.Append(PAConstants.IdentifierDelimiter);
            return sb.ToString();
        }

        return name;
    }
}
