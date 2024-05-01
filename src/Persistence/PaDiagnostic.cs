// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

/// <summary>
/// Represents a general diagnostic message.
/// </summary>
/// <remarks>
/// This class is modeled after MSBuild's diagnostic messages in order to provide consistent support for existing tooling that are used to parsing this format.
/// See: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-diagnostic-format-for-tasks?view=vs-2022
/// </remarks>
public record PaDiagnostic
{
    /// <summary>
    /// Creates a new instance with the specified <paramref name="category"/> and localized text, which may contain personal data or PII.
    /// </summary>
    /// <param name="text">The possibly localized text describing the error. This text MAY contain user personal data or PII.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public PaDiagnostic(PaDiagnosticCategory category, string text)
    {
        Category = category;
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public PaDiagnostic(PersistenceErrorCode errorCode, string text)
        : this(PaDiagnosticCategory.Error, text)
    {
        Code = $"PA{(int)errorCode}";
    }

    /// <summary>
    /// The origin of where this diagnostic message was detected.
    /// </summary>
    public PaDiagnosticOrigin? Origin { get; init; }

    /// <summary>
    /// The invariant subcategory used to classify the <see cref="Category"/> itself.
    /// </summary>
    public string? SubCategory { get; init; }

    /// <summary>
    /// The invariant category.
    /// </summary>
    public PaDiagnosticCategory Category { get; }

    /// <summary>
    /// Identifies the invariant error/warning code.
    /// Should NOT contain spaces.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Localized Pro-Dev user-friendly text that explains the error.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Returns the string representation of this instance that is safe to write to loggers.
    /// Namely, it excludes data which could possibly contain personal data or PII.
    /// </summary>
    public string ToLoggerSafeString()
    {
        return ToString(loggerSafeOnly: true);
    }

    /// <summary>
    /// WARNING: The string returned by this method IS NOT safe to write to loggers; use <see cref="ToLoggerSafeString"/> instead.
    /// </summary>
    public override string ToString()
    {
        return ToString(loggerSafeOnly: false);
    }

    private string ToString(bool loggerSafeOnly)
    {
        var sb = new StringBuilder();
        if (Origin != null)
        {
            Origin.AppendTo(sb, loggerSafeOnly);
            sb.Append(" : ");
        }

        if (!string.IsNullOrWhiteSpace(SubCategory))
        {
            sb.Append(SubCategory);
            sb.Append(' ');
        }

        // By convention, recommended to be lowercase
        sb.Append(Category.ToString().ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(Code))
        {
            sb.Append(' ');
            sb.Append(Code);
        }

        sb.Append(" : ");

        // PRIVACY: The error text is localized and MAY contain personal data (e.g. control names or formula values)
        if (!loggerSafeOnly)
        {
            sb.Append(Text);
        }

        return sb.ToString();
    }
}
