// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools.Parser
{
    internal class Parser
    {
        private string _fileName;
        private TokenStream _tokenizer;
        public ErrorContainer _errorContainer;

        public Parser(string fileName, string contents)
        {
            _tokenizer = new TokenStream(contents, fileName);
            _tokenizer.ValidateHeader();

            _fileName = fileName;
            _errorContainer = new ErrorContainer();
        }

        internal BlockNode ParseControl()
        {
            var spans = new List<SourceLocation>();

            var token = _tokenizer.GetNextToken();
            if (token.Kind != TokenKind.Control)
            {
                _errorContainer.AddError(token.Span, $"Unexpected token {token.Kind}, expected {TokenKind.Control}");
                return null;
            }

            spans.Add(token.Span);

            var identToken = _tokenizer.GetNextToken();
            if (identToken.Kind != TokenKind.Identifier)
            {
                _errorContainer.AddError(identToken.Span, $"Unexpected token {identToken.Kind}, expected {TokenKind.Identifier}");
                return null;
            }

            spans.Add(identToken.Span);

            var name = identToken.Content;

            var templateSeparator = _tokenizer.GetNextToken();
            if (templateSeparator.Kind != TokenKind.TemplateSeparator)
            {
                _errorContainer.AddError(templateSeparator.Span, $"Unexpected token {templateSeparator.Kind}, expected {TokenKind.TemplateSeparator}");
                return null;
            }

            spans.Add(templateSeparator.Span);

            var templateToken = _tokenizer.GetNextToken();
            if (templateToken.Kind != TokenKind.Identifier)
            {
                _errorContainer.AddError(templateToken.Span, $"Unexpected token {templateToken.Kind}, expected {TokenKind.Identifier}");
                return null;
            }           

            spans.Add(templateToken.Span);
            var templateNode = new TemplateNode() { TemplateName = templateToken.Content };
            var children = new List<BlockNode>();
            var properties = new List<PropertyNode>();

            var next = _tokenizer.GetNextToken();
            if (!_tokenizer.Eof)
            {
                if (next.Kind == TokenKind.VariantSeparator)
                {
                    var variantToken = _tokenizer.GetNextToken();
                    if (variantToken.Kind != TokenKind.Identifier)
                    {
                        _errorContainer.AddError(variantToken.Span, $"Unexpected token {variantToken.Kind}, expected {TokenKind.Identifier}");
                        return null;
                    }
                    templateNode.OptionalVariant = variantToken.Content;

                    next = _tokenizer.GetNextToken();
                }

                if (next.Kind != TokenKind.Indent)
                {
                    // Handle empty control special case
                    if (next.Kind == TokenKind.Control || next.Kind == TokenKind.Dedent)
                    {
                        _tokenizer.ReplaceToken(next);
                        return new BlockNode()
                        {
                            Name = new TypedNameNode()
                            {
                                Identifier = name,
                                Kind = templateNode
                            },
                            Children = children,
                            Functions = new List<FunctionNode>(),
                            Properties = properties,
                        };
                    }
                }

                next = _tokenizer.GetNextToken();

                while (!_tokenizer.Eof && next.Kind != TokenKind.Dedent)
                {
                    switch (next.Kind)
                    {
                        case TokenKind.Identifier:
                            var prop = ParseProperty(next.Content);
                            properties.Add(prop);
                            break;
                        case TokenKind.Control:
                            _tokenizer.ReplaceToken(next);
                            var child = ParseControl();
                            children.Add(child);
                            break;
                        default:
                            {
                                _errorContainer.AddError(next.Span, $"Unexpected token {next.Kind}, expected {TokenKind.Identifier} or {TokenKind.Control}");
                                return null;
                            }
                    }

                    next = _tokenizer.GetNextToken();
                }
            }

            return new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = name,
                    Kind = templateNode
                },
                Children = children,
                Functions = new List<FunctionNode>(),
                Properties = properties,
            };
        }

        private PropertyNode ParseProperty(string propertyName)
        {
            var propertySeparator = _tokenizer.GetNextToken();
            if (propertySeparator.Kind != TokenKind.PropertyStart)
            {
                _errorContainer.AddError(propertySeparator.Span, $"Unexpected token {propertySeparator.Kind}, expected {TokenKind.PropertyStart}");
                return new PropertyNode()
                {
                    Identifier = propertyName,
                    Expression = new ExpressionNode() { Expression = string.Empty }
                };
            }

            var ruleScript = _tokenizer.GetNextToken(expectedExpression: true);
            if (ruleScript.Kind != TokenKind.PAExpression)
            {
                _errorContainer.AddError(propertySeparator.Span, $"Unexpected token {propertySeparator.Kind}, expected expression");
                return new PropertyNode()
                {
                    Identifier = propertyName,
                    Expression = new ExpressionNode() { Expression = string.Empty }
                };
            }

            return new PropertyNode()
            {
                Identifier = propertyName,
                Expression = new ExpressionNode() { Expression = ruleScript.Content }
            };
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
