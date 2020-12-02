// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.PowerPlatform.Formulas.Tools.Parser
{
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

        private Regex _controlDefRegex = new Regex(@"^(.+?)\s+As\s+(['_A-Za-z0-9]+)(\.(\S+))?$");


        // Parse the control definition line. Something like:
        //   Screen1 as Screen
        //   Label1 As Label.Variant
        private TypedNameNode ParseControlDef(YamlToken token)
        {
            string line = token.Property;

            var m = _controlDefRegex.Match(line);
            if (!m.Success)
            {
                _errorContainer.ParseError(token.Span, "Can't parse control definition");
                throw new DocumentException();
            }
            // p.Property;
            // Label1 As Label.Variant:
            string controlName = CharacterUtils.UnEscapeName(m.Groups[1].Value);
            string templateName = CharacterUtils.UnEscapeName(m.Groups[2].Value);
            string variantName = m.Groups[4].Success ? CharacterUtils.UnEscapeName(m.Groups[4].Value) : null;

            return new TypedNameNode
            {
                Identifier = controlName,
                Kind = new TypeNode
                {
                    TypeName = templateName,
                    OptionalVariant = variantName
                }
            };
        }

        public BlockNode ParseControl()
        {
            var p = _yaml.ReadNext();
            return ParseNestedControl(p);
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
                        block.Properties.Add(new PropertyNode
                        {
                            Identifier = CharacterUtils.UnEscapeName(p.Property),
                            Expression = new ExpressionNode
                            {
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
                            if (_errorContainer.HasErrors
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
                        return null;
                }
            }
        }

        // See https://github.com/microsoft/PowerApps-Language-Tooling/blob/gregli-docs/docs/syntax.md#simple-function-definition
        private FunctionNode ParseFunctionDef(YamlToken p)
        {
            var paramRegex = new Regex(@"^(.+?)\s+As\s+(['_A-Za-z0-9]+)");
            var funcNameRegex = new Regex(@"^(.+?)\(");
            var line = p.Property;
            var m = funcNameRegex.Match(line);

            if (!m.Success)
            {
                _errorContainer.ParseError(p.Span, $"Can't parse Function definition");
                return null;
            }

            var funcName = m.Groups[1].Value;
            var functionNode = new FunctionNode() { Identifier = funcName };

            line = line.Substring(m.Length);

            m = paramRegex.Match(line);
            while (m.Success)
            {
                string argName = CharacterUtils.UnEscapeName(m.Groups[1].Value);
                string kindName = CharacterUtils.UnEscapeName(m.Groups[2].Value);

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
                return null;
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
                        return null;
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
                            return null;
                        }
                        break;
                    case YamlTokenKind.Error:
                        _errorContainer.ParseError(p.Span, p.Value);
                        return null;

                    default:
                        _errorContainer.ParseError(p.Span, $"Unexpected yaml token: {p}");
                        return null;
                }
            }
        }

        private bool IsControlStart(string line) => _controlDefRegex.IsMatch(line);
    }
}
