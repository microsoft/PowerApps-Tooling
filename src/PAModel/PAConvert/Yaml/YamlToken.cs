// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

/// <summary>
/// A yaml token in the file. 
/// </summary>
internal class YamlToken
{
    internal static YamlToken EndObj = new() { Kind = YamlTokenKind.EndObj };
    internal static YamlToken EndOfFile = new() { Kind = YamlTokenKind.EndOfFile };

    public YamlTokenKind Kind { get; set; }

    /// <summary>
    /// The name of a property. Valid for Property and StartObj kinds. 
    /// </summary>
    public string Property { get; set; }

    /// <summary>
    /// The contents of a Property. Valid for Property kinds. 
    /// </summary>
    public string Value { get; set; }

    // Used for error reporting. 
    public SourceLocation Span { get; set; }


    private YamlToken() { }

    public static YamlToken NewStartObj(SourceLocation span, string name)
    {
        return new()
        {
            Kind = YamlTokenKind.StartObj,
            Span = span,
            Property = name
        };
    }

    public static YamlToken NewProperty(SourceLocation span, string name, string value)
    {
        return new()
        {
            Kind = YamlTokenKind.Property,
            Span = span,
            Property = name,
            Value = value
        };
    }


    public static YamlToken NewError(SourceLocation span, string message)
    {
        return new()
        {
            Kind = YamlTokenKind.Error,
            Span = span,
            Value = message
        };
    }

    public override string ToString()
    {
        return Kind switch
        {
            YamlTokenKind.Property => $"{Property}={Value}",
            YamlTokenKind.StartObj => $"{Property}:",
            YamlTokenKind.Error => $"Yaml error: {Span.FileName}:{Span.StartLine},{Span.StartChar} {Value}",
            _ => $"<{Kind}>",
        };
    }
}
