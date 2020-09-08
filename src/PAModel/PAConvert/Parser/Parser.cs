// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PAModel.PAConvert.Parser
{
    public class Parser
    {
        private string _content;
        private TokenStream _tokenizer;
        private Dictionary<string, ControlInfoJson.Item> _controlStates;
        private Dictionary<string, ControlInfoJson.Template> _templates;

        public Parser(string contents, Dictionary<string, ControlInfoJson.Item> controlStates, Dictionary<string, ControlInfoJson.Template> templates)
        {
            var header = "//! PAFile:0.1";

            if (!contents.StartsWith(header))
            {
                throw new InvalidOperationException($"Illegal pa source file. Missing header");
            }
            _content = contents.Substring(header.Length+1);
            _tokenizer = new TokenStream(_content);

            _controlStates = controlStates;
            _templates = templates;
        }

        internal ControlInfoJson.Item ParseControl(string parent = "", bool isComponent = false)
        {
            if (parent == string.Empty)
            {
                var token = _tokenizer.GetNextToken();
                if (token.Kind != TokenKind.Control)
                    throw new InvalidOperationException($"Unexpected token {token.Kind}, expected {TokenKind.Control}");
                isComponent = token.Content == PAConstants.ComponentKeyword;
            }
            
            var identToken = _tokenizer.GetNextToken();
            if (identToken.Kind != TokenKind.Identifier)
                throw new InvalidOperationException($"Unexpected token {identToken.Kind}, expected {TokenKind.Identifier}");

            var name = identToken.Content;
            if (!_controlStates.TryGetValue(name, out var control))
                control = new ControlInfoJson.Item(); // Should have an arg for defaults maybe?

            control.Name = name;
            if (parent != string.Empty)
                control.Parent = parent;


            var templateSeparator = _tokenizer.GetNextToken();
            if (templateSeparator.Kind != TokenKind.TemplateSeparator)
                throw new InvalidOperationException($"Unexpected token {templateSeparator.Kind}, expected {TokenKind.TemplateSeparator}");

            var templateToken = _tokenizer.GetNextToken();
            if (templateToken.Kind != TokenKind.Identifier)
                throw new InvalidOperationException($"Unexpected token {templateToken.Kind}, expected {TokenKind.Identifier}");

            if (!_templates.TryGetValue(templateToken.Content, out var template))
            {
                template = new ControlInfoJson.Template(); // This seems like a problem, maybe we can't recreate templates without npm ref?
                template.Name = templateToken.Content;
            }
            else
            {
                template = new ControlInfoJson.Template(template);
            }

            control.Template = template;
            if (isComponent && control.Template.IsComponentDefinition != null)
            {
                control.Template.IsComponentDefinition = true;
                control.Template.ComponentDefinitionInfo = null;
            }

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
                // Handle empty control special case
                if (next.Kind == TokenKind.Control)
                {
                    _tokenizer.ReplaceToken(next);
                    control.Children = new ControlInfoJson.Item[0];
                    foreach (var rule in control.Rules)
                        rule.InvariantScript = string.Empty;
                }

                return control;
            }

            next = _tokenizer.GetNextToken();

            var paRules = new Dictionary<string, string>();
            var children = new List<ControlInfoJson.Item>();

            while (!_tokenizer.Eof && next.Kind != TokenKind.Dedent)
            {
                switch (next.Kind)
                {
                    case TokenKind.Identifier:
                        var rule = ParseRule(next.Content);
                        paRules.Add(rule.propertyName, rule.script);
                        break;
                    case TokenKind.Control:
                        var child = ParseControl(parent: control.Name, next.Content == PAConstants.ComponentKeyword);
                        children.Add(child);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected token {next.Kind}");
                }

                next = _tokenizer.GetNextToken();
            }

            control.Children = children.ToArray();

            foreach (var rule in control.Rules)
            {
                if (paRules.TryGetValue(rule.Property, out var script))
                {
                    rule.InvariantScript = script;
                    paRules.Remove(rule.Property);
                }
                else
                {
                    rule.InvariantScript = string.Empty;
                }
            }
            if (paRules.Any())
            {
                var rulesList = control.Rules.ToList();
                foreach (var rulePair in paRules)
                { 
                    // Needs sensible defaults for other props (maybe from template
                    rulesList.Add(new ControlInfoJson.RuleEntry() { Property = rulePair.Key, InvariantScript = rulePair.Value }); 
                }
                control.Rules = rulesList.ToArray();
            }

            return control;
        }

        private (string propertyName, string script) ParseRule(string propertyName)
        {   
            var propertySeparator = _tokenizer.GetNextToken();
            if (propertySeparator.Kind != TokenKind.PropertyStart)
                throw new InvalidOperationException($"Unexpected token {propertySeparator.Kind}, expected {TokenKind.PropertyStart}");

            var ruleScript = _tokenizer.GetNextToken(expectedExpression: true);
            if (ruleScript.Kind != TokenKind.PAExpression)
                throw new InvalidOperationException($"Unexpected token {ruleScript.Kind}, expected {TokenKind.PAExpression}");

            return (propertyName, ruleScript.Content);
        }
    }
}
