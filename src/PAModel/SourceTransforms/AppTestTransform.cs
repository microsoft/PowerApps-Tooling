using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms
{
    internal class AppTestTransform : IControlTemplateTransform
    {
        private class TestStepsMetadataJson
        {
            public string Description { get; set; }
            public string Rule { get; set; }
            public string ScreenId { get; set; } = null;
        }

        private static readonly IEnumerable<string> _targets = new List<string>() { "TestCase" };
        public IEnumerable<string> TargetTemplates => _targets;

        private const string _metadataPropName = "TestStepsMetadata";
        private string _testStepTemplateName;

        // Key is UniqueId, Value is ScreenName
        private IList<KeyValuePair<string, string>> _screenIdToScreenName;

        public AppTestTransform(TemplateStore templateStore, EditorStateStore stateStore)
        {
            _testStepTemplateName = "TestStep";

            int i = 1;
            while (templateStore.TryGetTemplate(_testStepTemplateName, out _))
                _testStepTemplateName = "TestStep" + i;

            _screenIdToScreenName = stateStore.Contents
                .Where(state => state.TopParentName == state.Name)
                .Select(state => new KeyValuePair<string, string>(state.UniqueId, state.Name)).ToList();
        }

        public void AfterRead(BlockNode control)
        {
            var properties = control.Properties.ToDictionary(prop => prop.Identifier);
            if (!properties.TryGetValue(_metadataPropName, out var metadataProperty))
                throw new InvalidOperationException($"Unable to find TestStepsMetadata property for TestCase {control.Name.Identifier}");

            properties.Remove(_metadataPropName);
            var metadataJsonString = Utility.UnEscapePAString(metadataProperty.Expression.Expression);
            var testStepsMetadata = JsonSerializer.Deserialize<List<TestStepsMetadataJson>>(metadataJsonString);
            var newChildren = new List<BlockNode>();

            foreach (var testStep in testStepsMetadata)
            {
                if (!properties.TryGetValue(testStep.Rule, out var testStepProp))
                    throw new InvalidOperationException($"Unable to find corresponding property for test step {testStep.Rule} in {control.Name.Identifier}");

                if (testStep.ScreenId != null)
                if (!_screenIdToScreenName.ToDictionary(kvp => kvp.Key, kvp => kvp.Value).TryGetValue(testStep.ScreenId, out var screenName))
                    throw new InvalidOperationException($"ScreenId referenced by TestStep {testStep.Rule} in {control.Name.Identifier} could not be found");

                var childProperties = new List<PropertyNode>()
                    {
                        new PropertyNode()
                        {
                            Identifier = "Description",
                            Expression = new ExpressionNode()
                            {
                                Expression = Utility.EscapePAString(testStep.Description)
                            }
                        },
                        new PropertyNode()
                        {
                            Identifier = "Value",
                            Expression = testStepProp.Expression
                        }
                    };

                if (testStep.ScreenId != null)
                {
                    if (!_screenIdToScreenName.ToDictionary(kvp => kvp.Key, kvp => kvp.Value).TryGetValue(testStep.ScreenId, out var screenName))
                        throw new InvalidOperationException($"ScreenId referenced by TestStep {testStep.Rule} in {control.Name.Identifier} could not be found");

                    childProperties.Add(new PropertyNode()
                    {
                        Identifier = "Screen",
                        Expression = new ExpressionNode()
                        {
                            Expression = screenName
                        }
                    });
                }

                var testStepControl = new BlockNode()
                {
                    Name = new TypedNameNode()
                    {
                        Identifier = testStep.Rule,
                        Kind = new TypeNode() { TypeName = _testStepTemplateName }
                    },
                    Properties = childProperties
                };

                properties.Remove(testStep.Rule);
                newChildren.Add(testStepControl);
            }
            control.Properties = properties.Values.ToList();
            control.Children = newChildren;
        }

        public void BeforeWrite(BlockNode control)
        {
            var testStepsMetadata = new List<TestStepsMetadataJson>();

            foreach (var child in control.Children)
            {
                var propName = child.Name.Identifier;
                if (child.Name.Kind.TypeName != _testStepTemplateName)
                    throw new InvalidOperationException($"Only controls of type {_testStepTemplateName} are valid children of a TestCase");
                if (child.Properties.Count > 3)
                    throw new InvalidOperationException($"Test Step {propName} has unexpected properties");
                var descriptionProp = child.Properties.FirstOrDefault(prop => prop.Identifier == "Description");
                if (descriptionProp == null)
                    throw new InvalidOperationException($"Test Step {propName} is missing a Description property");
                var valueProp = child.Properties.FirstOrDefault(prop => prop.Identifier == "Value");
                if (valueProp == null)
                    throw new InvalidOperationException($"Test Step {propName} is missing a Value property");
                var screenProp = child.Properties.FirstOrDefault(prop => prop.Identifier == "Screen");

                string screenId = null;
                // Lookup screenID by Name
                if (screenProp != null && !_screenIdToScreenName.ToDictionary(kvp => kvp.Value, kvp => kvp.Key).TryGetValue(screenProp.Expression.Expression, out screenId))
                    throw new InvalidOperationException($"Test Step {propName} references screen {screenProp.Expression.Expression} that is not present in the app");

                testStepsMetadata.Add(new TestStepsMetadataJson()
                {
                    Description = Utility.UnEscapePAString(descriptionProp.Expression.Expression),
                    Rule = propName,
                    ScreenId = screenId
                });

                control.Properties.Add(new PropertyNode()
                {
                    Expression = valueProp.Expression,
                    Identifier = propName
                });
            }

            var testStepMetadataStr = JsonSerializer.Serialize<List<TestStepsMetadataJson>>(testStepsMetadata, new JsonSerializerOptions() {Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            control.Properties.Add(new PropertyNode()
            {
                Expression = new ExpressionNode() { Expression = Utility.EscapePAString(testStepMetadataStr) },
                Identifier = _metadataPropName
            });
            control.Children = new List<BlockNode>();
        }
    }
}
