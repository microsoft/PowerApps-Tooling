// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
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
        private string _content;
        private string _fileName;
        private TokenStream _tokenizer;
        // Key is control name
        private Dictionary<string, ControlInfoJson.Item> _controlStates;

        // Keys are template name
        private Dictionary<string, ControlInfoJson.Template> _templates;
        private Dictionary<string, ControlTemplate> _templateDefaults;
        private readonly Theme _theme;

        public ErrorContainer _errorContainer;

        public Parser(string fileName, string contents,
            Dictionary<string, ControlInfoJson.Item> controlStates,
            Dictionary<string, ControlInfoJson.Template> templates,
            Dictionary<string, ControlTemplate> templateDefaults,
            Theme theme)
        {
            var header = "//! PAFile:0.1";

            if (!contents.StartsWith(header))
            {
                throw new InvalidOperationException($"Illegal pa source file. Missing header");
            }
            _content = contents.Substring(header.Length + 1);
            _fileName = fileName;
            _tokenizer = new TokenStream(_content);
            _errorContainer = new ErrorContainer();

            _controlStates = controlStates;
            _templates = templates;
            _templateDefaults = templateDefaults;
            _theme = theme;
        }

        internal ControlInfoJson.Item ParseControl(string parent = "", bool isComponent = false)
        {
            if (parent == string.Empty)
            {
                var token = _tokenizer.GetNextToken();
                if (token.Kind != TokenKind.Control)
                {
                    _errorContainer.AddError(token.Span, $"Unexpected token {token.Kind}, expected {TokenKind.Control}");
                    return null;
                }
                isComponent = token.Content == PAConstants.ComponentKeyword;
            }

            var identToken = _tokenizer.GetNextToken();
            if (identToken.Kind != TokenKind.Identifier)
            {
                _errorContainer.AddError(identToken.Span, $"Unexpected token {identToken.Kind}, expected {TokenKind.Identifier}");
                return null;
            }

            var name = identToken.Content;


            var templateSeparator = _tokenizer.GetNextToken();
            if (templateSeparator.Kind != TokenKind.TemplateSeparator)
            {
                _errorContainer.AddError(templateSeparator.Span, $"Unexpected token {templateSeparator.Kind}, expected {TokenKind.TemplateSeparator}");
                return null;
            }

            var templateToken = _tokenizer.GetNextToken();
            if (templateToken.Kind != TokenKind.Identifier)
            {
                _errorContainer.AddError(templateToken.Span, $"Unexpected token {templateToken.Kind}, expected {TokenKind.Identifier}");
                return null;
            }


            if (!_templateDefaults.TryGetValue(templateToken.Content, out var controlTemplate))
                controlTemplate = null;

            ControlInfoJson.Template template = default;
            if (!_templates?.TryGetValue(templateToken.Content, out template) ?? true)
            {
                template = new ControlInfoJson.Template();
                template.Name = templateToken.Content;
                // Try recreating template using template defaults
                if (controlTemplate != null)
                {
                    template.Id = controlTemplate.Id;
                    template.Version = controlTemplate.Version;
                    template.IsComponentDefinition = false;
                    template.LastModifiedTimestamp = "0";
                    template.ExtensionData = new Dictionary<string, object>();
                    template.ExtensionData.Add("FirstParty", true);
                    template.ExtensionData.Add("IsCustomGroupControlTemplate", false);
                    template.ExtensionData.Add("CustomGroupControlTemplateName", "");
                    template.ExtensionData.Add("OverridableProperties", new object());
                }
            }
            else
            {
                template = new ControlInfoJson.Template(template);
            }

            ControlInfoJson.Item control = default;
            if (!_controlStates?.TryGetValue(name, out control) ?? true)
            {
                control = ControlInfoJson.Item.CreateDefaultControl(controlTemplate);
            }

            control.Name = name;
            if (parent != string.Empty)
                control.Parent = parent;

            control.Template = template;
            if (isComponent && control.Template.IsComponentDefinition != null)
            {
                control.Template.IsComponentDefinition = true;
                control.Template.ComponentDefinitionInfo = null;
            }

            var paRules = new Dictionary<string, string>(); // PropertyName --> Script
            var children = new List<ControlInfoJson.Item>();

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
                            {
                                _errorContainer.AddError(next.Span, $"Unexpected token {next.Kind}, expected {TokenKind.Identifier} or {TokenKind.Control}");
                                return null;
                            }
                    }

                    next = _tokenizer.GetNextToken();
                }

            }

            control.Children = children.ToArray();

            // Dict of property => default expression
            var defaulter = new DefaultRuleHelper(control, controlTemplate, _theme);
            var defaults = defaulter.GetDefaultRules();


            foreach (var rule in control.Rules)
            {
                if (paRules.TryGetValue(rule.Property, out var script))
                {
                    rule.InvariantScript = script;
                    paRules.Remove(rule.Property);
                }
                else
                {
                    var defaultValue = string.Empty;
                    // Restore default property values that weren't in the .pa file
                    if (defaults?.TryGetValue(rule.Property, out defaultValue) ?? false)
                        rule.InvariantScript = defaultValue;
                    else
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
            {
                _errorContainer.AddError(propertySeparator.Span, $"Unexpected token {propertySeparator.Kind}, expected {TokenKind.PropertyStart}");
                return (propertyName, null);
            }

            var ruleScript = _tokenizer.GetNextToken(expectedExpression: true);
            if (ruleScript.Kind != TokenKind.PAExpression)
            {
                _errorContainer.AddError(propertySeparator.Span, $"Unexpected token {propertySeparator.Kind}, expected expression");
                return (propertyName, null);
            }

            return (propertyName, ruleScript.Content);
        }

        public bool HasErrors() => _errorContainer.HasErrors();

        public void WriteErrors()
        {
            foreach (var error in _errorContainer.Errors())
            {
                Console.WriteLine($"{_fileName}:{error.Span.Min}-{error.Span.Lim}   {error.Message}");
            }
        }
    }
}
