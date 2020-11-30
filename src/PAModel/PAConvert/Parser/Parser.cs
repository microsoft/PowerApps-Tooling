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
        private string _fileName;        
        public ErrorContainer _errorContainer;

        private YamlLexer _yaml;

        public Parser(string fileName, string contents)
        {
            _fileName = fileName;
            _errorContainer = new ErrorContainer();

            _yaml = new YamlLexer(new StringReader(contents), fileName);

        }

        // Parse the control definition line. Something like:
        //   Screen1 as Screen
        //   Label1 As Label.Variant
        private TypedNameNode ParseControlDef(string line)
        {
            // $$$ use real parser for this?
            Regex r = new Regex(@"^(.+?)\s+As\s+(['_A-Za-z0-9]+)(\.(\S+))?$");

            var m = r.Match(line);
            if (!m.Success)
            {
                return null;
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
                // _errorContainer.AddError(token.Span, $"Unexpected token {token.Kind}, expected {TokenKind.Control}");
                return null;
            }

            var controlDef = ParseControlDef(p.Property);
            if (controlDef == null)
            {
                _errorContainer.AddError(p.Span, "Can't parse control definition");
                return null;
            }

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
                        if (_errorContainer.HasErrors())
                        {
                            return null;
                        }
                        block.Children.Add(childNode);
                        break;

                    case YamlTokenKind.Error:
                        _errorContainer.AddError(p.Span, p.Value);
                        return null;

                    default:
                        _errorContainer.AddError(p.Span, $"Unexpected yaml token: {p}");
                        return null;
                }
            }
        }

        public bool HasErrors() => _errorContainer.HasErrors();

        public void WriteErrors()
        {
            foreach (var error in _errorContainer.Errors())
            {
                Console.WriteLine($"{_fileName}:{error.Span.StartLine}:{error.Span.StartChar}-{error.Span.EndLine}:{error.Span.EndChar}   {error.Message}");
            }
        }
    }
}
