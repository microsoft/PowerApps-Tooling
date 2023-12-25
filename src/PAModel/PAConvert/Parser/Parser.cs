// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.PowerPlatform.Formulas.Tools.Parser;

internal class Parser
{
    private readonly string _fileName;
    public readonly ErrorContainer _errorContainer;

    private readonly YamlLexer _yaml;

    public Parser(string fileName, string contents, ErrorContainer errors)
    {
        _fileName = fileName;
        _errorContainer = errors;

        _yaml = new YamlLexer(new StringReader(contents), fileName);

    }

    // Parse the control definition line. Something like:
    //   Screen1 as Screen
    //   Label1 As Label.Variant
    private TypedNameNode ParseControlDef(YamlToken token)
    {
        string line = token.Property;

        if (!TryParseControlDefCore(line, out var controlName, out var templateName, out var variantName))
        {
            _errorContainer.ParseError(token.Span, "Can't parse control definition");
            throw new DocumentException();
        }

        return new TypedNameNode
        {
            Identifier = controlName,
            SourceSpan = token.Span,
            Kind = new TypeNode
            {
                TypeName = templateName,
                OptionalVariant = variantName
            }
        };
    }

    internal static bool TryParseControlDefCore(string line, out string ctrlName, out string templateName, out string variantName)
    {
        ctrlName = templateName = variantName = null;
        if (!TryParseIdent(line, out var parsedIdent, out var length))
            return false;
        ctrlName = parsedIdent;

        line = line.Substring(length);
        if (!line.StartsWith(" "))
            return false;
        line = line.TrimStart();

        if (!line.StartsWith("As "))
            return false;

        line = line.Substring(2).TrimStart();

        if (!TryParseIdent(line, out parsedIdent, out length))
            return false;

        templateName = parsedIdent;

        if (length == line.Length)
            return true;

        line = line.Substring(length);
        if (!line.StartsWith("."))
            return false;
        line = line.Substring(1);

        if (!TryParseIdent(line, out parsedIdent, out length))
            return false;
        variantName = parsedIdent;
        if (length == line.Length)
            return true;

        return false;
    }

    internal static  bool TryParseIdent(string source, out string parsed, out int length)
    {
        length = 0;
        parsed = null;
        if (source.Length == 0)
            return false;

        var i = 0;
        var result = new StringBuilder();
        var hasDelimiterStart = CharacterUtils.IsIdentDelimiter(source[i]);
        var hasDelimiterEnd = false;

        if (!hasDelimiterStart)
        {
            // Simple identifier.
            while (i < source.Length && CharacterUtils.IsSimpleIdentCh(source[i]))
            {
                result.Append(source[i]);
                ++i;
            }
            parsed = result.ToString();
            length = i;
            return true;
        }

        // Delimited identifier.
        ++i;

        // Accept any characters up to the next unescaped identifier delimiter.
        for (; ;)
        {
            if (i >= source.Length)
                break;

            if (CharacterUtils.IsIdentDelimiter(source[i]))
            {
                if (i + 1 < source.Length && CharacterUtils.IsIdentDelimiter(source[i + 1]))
                {
                    // Escaped delimiter.
                    result.Append(source[i]);
                    i += 2;
                }
                else
                {
                    // End of the identifier.
                    hasDelimiterEnd = true;
                    ++i;
                    break;
                }
            }
            else
            {
                result.Append(source[i]);
                ++i;
            }
        }

        if (hasDelimiterStart == hasDelimiterEnd && result.Length > 0)
        {
            length = i;
            parsed = result.ToString();
            return true;
        }
        return false;
    }

    public BlockNode ParseControl(bool isComponent)
    {
        _yaml.IsComponent = isComponent;
        var p = _yaml.ReadNext();
        var control = ParseNestedControl(p);

        if (_yaml._commentStrippedWarning.HasValue)
        {
            this._errorContainer.YamlWontRoundTrip(_yaml._commentStrippedWarning.Value, "Yaml comments don't roundtrip.");
        }

        return control;
    }

    private BlockNode ParseNestedControl(YamlToken p)
    {
        if (p.Kind != YamlTokenKind.StartObj)
        {
            _errorContainer.ParseError(p.Span, $"Unexpected token {p}");
            throw new DocumentException();
        }

        var controlDef = ParseControlDef(p);

        var block = new BlockNode
        {
            SourceSpan = p.Span,
            Name = controlDef
        };

        while (true)
        {
            p = _yaml.ReadNext();
            switch (p.Kind)
            {
                case YamlTokenKind.EndObj:
                    return block;

                case YamlTokenKind.Property:
                    // Yaml parser only gives back a single span for property name and value. 
                    block.Properties.Add(new PropertyNode
                    {
                        SourceSpan = p.Span,
                        Identifier = CharacterUtils.UnEscapeName(p.Property, _errorContainer),
                        Expression = new ExpressionNode
                        {
                            SourceSpan = p.Span,
                            Expression = p.Value
                        }
                    });
                    break;

                // StartObj can either be a Control or Function def
                case YamlTokenKind.StartObj:
                    if (IsControlStart(p.Property))
                    {
                        var childNode = ParseNestedControl(p);
                        if (_errorContainer.HasErrors)
                        {
                            return null;
                        }
                        block.Children.Add(childNode);
                    }
                    else
                    {
                        var functionNode = ParseFunctionDef(p);
                        if (_errorContainer.HasErrors)
                        {
                            return null;
                        }
                        block.Functions.Add(functionNode);
                    }
                    break;

                case YamlTokenKind.Error:
                    _errorContainer.ParseError(p.Span, p.Value);
                    return null;

                default:
                    _errorContainer.ParseError(p.Span, $"Unexpected yaml token: {p}");
                    throw new DocumentException();
            }
        }
    }

    // Name ( Parameter-Name As Data-type [ , Parameter-Name As Data-type ... ] ) :  (ThisProperty or Parameter-Name) :   Metadata-Name : Metadata-Value   ...  ...
    // Currently iterating on what fields are present in the property metadata blocks
    // Right now, only Default is permitted
    private FunctionNode ParseFunctionDef(YamlToken p)
    {
        // Validation here mirrors validation in PA-Client
        var paramRegex = new Regex(@"^([\p{L}\p{Nd}\p{Mn}\p{Mc}\p{Pc}\p{Cf}]+?)\s+As\s+([\p{L}\p{Nd}\p{Mn}\p{Mc}\p{Pc}\p{Cf}]+)");
        var funcNameRegex = new Regex(@"^([\p{L}\p{Nd}\p{Mn}\p{Mc}\p{Pc}\p{Cf}]+?)\(");
        var line = p.Property;
        var m = funcNameRegex.Match(line);

        if (!m.Success)
        {
            _errorContainer.ParseError(p.Span, $"Can't parse Function definition");
            throw new DocumentException();
        }

        var funcName = m.Groups[1].Value;
        var functionNode = new FunctionNode() { Identifier = funcName };

        line = line.Substring(m.Length);

        m = paramRegex.Match(line);
        while (m.Success)
        {
            string argName = CharacterUtils.UnEscapeName(m.Groups[1].Value, _errorContainer);
            string kindName = CharacterUtils.UnEscapeName(m.Groups[2].Value, _errorContainer);

            functionNode.Args.Add(new TypedNameNode
            {
                Identifier = argName,
                Kind = new TypeNode
                {
                    TypeName = kindName
                }
            });

            line = line.Substring(m.Length).TrimStart(',', ' ');
            m = paramRegex.Match(line);
        }

        if (line != ")")
        {
            _errorContainer.ParseError(p.Span, $"Missing closing ')' in function definition");
            throw new DocumentException();
        }

        while (true)
        {
            p = _yaml.ReadNext();
            switch (p.Kind)
            {
                case YamlTokenKind.EndObj:
                    return functionNode;

                // Expecting N+1 child objs where one is ThisProperty and the others N are the args 
                case YamlTokenKind.StartObj:
                    functionNode.Metadata.Add(ParseArgMetadataBlock(p));
                    break;
                case YamlTokenKind.Error:
                    _errorContainer.ParseError(p.Span, p.Value);
                    return null;

                default:
                    _errorContainer.ParseError(p.Span, $"Unexpected yaml token: {p}");
                    throw new DocumentException();
            }
        }
    }

    private ArgMetadataBlockNode ParseArgMetadataBlock(YamlToken p)
    {
        var argNode = new ArgMetadataBlockNode() { Identifier = p.Property };
        while (true)
        {
            p = _yaml.ReadNext();
            switch (p.Kind)
            {
                case YamlTokenKind.EndObj:
                    return argNode;

                case YamlTokenKind.Property:
                    if (p.Property == nameof(ArgMetadataBlockNode.Default))
                        argNode.Default = new ExpressionNode() { Expression = p.Value };
                    else
                    {
                        _errorContainer.ParseError(p.Span, $"Unexpected key in function definition: {p}");
                        throw new DocumentException();
                    }
                    break;
                case YamlTokenKind.Error:
                    _errorContainer.ParseError(p.Span, p.Value);
                    return null;

                default:
                    _errorContainer.ParseError(p.Span, $"Unexpected yaml token: {p}");
                    throw new DocumentException();
            }
        }
    }

    private bool IsControlStart(string line)
    {
        if (!TryParseIdent(line, out var parsed, out var length))
            return false;
        line = line.Substring(length).TrimStart();
        return line.StartsWith("As");
    }
}
