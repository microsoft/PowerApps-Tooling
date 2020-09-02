using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PAModel.PAConvert.Parser
{
    public class Parser
    {
        private string _content;
        private TokenStream _tokenizer;

        public Parser(string contents)
        {
            var header = "//! PAFile:0.1";

            if (!contents.StartsWith(header))
            {
                throw new InvalidOperationException($"Illegal pa source file. Missing header");
            }
            _content = contents.Substring(header.Length+1);
            _tokenizer = new TokenStream(_content);
        }

        internal ControlInfoJson.Item ParseControl(bool skipStart = false)
        {
            var control = new ControlInfoJson.Item();
            if (!skipStart)
            {
                var token = _tokenizer.GetNextToken();
                if (token.Kind != TokenKind.Control)
                    throw new InvalidOperationException($"Unexpected token {token.Kind}, expected {TokenKind.Control}");
            }
            var identToken = _tokenizer.GetNextToken();
            if (identToken.Kind != TokenKind.Identifier)
                throw new InvalidOperationException($"Unexpected token {identToken.Kind}, expected {TokenKind.Identifier}");
            control.Name = identToken.Content;

            var templateSeparator = _tokenizer.GetNextToken();
            if (templateSeparator.Kind != TokenKind.TemplateSeparator)
                throw new InvalidOperationException($"Unexpected token {templateSeparator.Kind}, expected {TokenKind.TemplateSeparator}");

            var templateToken = _tokenizer.GetNextToken();
            if (templateToken.Kind != TokenKind.Identifier)
                throw new InvalidOperationException($"Unexpected token {templateToken.Kind}, expected {TokenKind.Identifier}");
            var template = new ControlInfoJson.Template();
            template.Name = templateToken.Content;
            control.Template = template;

            var next = _tokenizer.GetNextToken();
            if (next.Kind == TokenKind.VariantSeparator)
            {
                var variantToken = _tokenizer.GetNextToken();
                if (variantToken.Kind != TokenKind.Identifier)
                    throw new InvalidOperationException($"Unexpected token {variantToken.Kind}, expected {TokenKind.Identifier}");
                control.VariantName = variantToken.Content;

                next = _tokenizer.GetNextToken();
            }

            if (next.Kind != TokenKind.Indent)
            {
                return control;
            }

            next = _tokenizer.GetNextToken();

            var rules = new List<ControlInfoJson.RuleEntry>();
            var children = new List<ControlInfoJson.Item>();

            while (!_tokenizer.Eof && next.Kind != TokenKind.Dedent)
            {
                switch (next.Kind)
                {
                    case TokenKind.Identifier:
                        var rule = ParseRule(next.Content);
                        rules.Add(rule);
                        break;
                    case TokenKind.Control:
                        var child = ParseControl(skipStart: true);
                        children.Add(child);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected token {next.Kind}");
                }
                next = _tokenizer.GetNextToken();
            }

            control.Children = children.ToArray();
            control.Rules = rules.ToArray();

            return control;
        }

        private ControlInfoJson.RuleEntry ParseRule(string propertyName)
        {
            var rule = new ControlInfoJson.RuleEntry();
            rule.Property = propertyName;

            var propertySeparator = _tokenizer.GetNextToken();
            if (propertySeparator.Kind != TokenKind.PropertyStart)
                throw new InvalidOperationException($"Unexpected token {propertySeparator.Kind}, expected {TokenKind.PropertyStart}");

            var ruleScript = _tokenizer.GetNextToken(expectedExpression: true);
            if (ruleScript.Kind != TokenKind.PAExpression)
                throw new InvalidOperationException($"Unexpected token {ruleScript.Kind}, expected {TokenKind.PAExpression}");

            rule.InvariantScript = ruleScript.Content;

            return rule;
        }
    }
}
