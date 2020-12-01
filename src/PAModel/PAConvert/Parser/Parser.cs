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

        // Parse the control definition line. Something like:
        //   Screen1 as Screen
        //   Label1 As Label.Variant
        private TypedNameNode ParseControlDef(YamlToken token)
        {
            string line = token.Property;

            // $$$ use real parser for this?
            Regex r = new Regex(@"^(.+?)\s+As\s+(['_A-Za-z0-9]+)(\.(\S+))?$");

            var m = r.Match(line);
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
                Kind = new TemplateNode
                {
                    TemplateName = templateName,
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

                    case YamlTokenKind.StartObj:
                        var childNode = ParseNestedControl(p);
                        if (_errorContainer.HasErrors)
                        {
                            return null;
                        }
                        block.Children.Add(childNode);
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
    }
}
