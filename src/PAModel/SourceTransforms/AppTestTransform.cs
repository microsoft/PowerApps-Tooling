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
        }

        public string TargetTemplate { get; } = "TestCase";

        private const string _metadataPropName = "TestStepsMetadata";
        private string _testStepTemplateName;

        public AppTestTransform(TemplateStore templateStore)
        {
            _testStepTemplateName = "TestStep";

            int i = 1;
            while (templateStore.TryGetTemplate(_testStepTemplateName, out _))
                _testStepTemplateName = "TestStep" + i;
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

                var testStepControl = new BlockNode()
                {
                    Name = new TypedNameNode()
                    {
                        Identifier = testStep.Rule,
                        Kind = new TypeNode() { TypeName = _testStepTemplateName }
                    },
                    Properties = new List<PropertyNode>()
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
                    }
                };

                properties.Remove(testStep.Rule);
                newChildren.Add(testStepControl);
            }
            control.Properties = properties.Values.ToList();
            control.Children = newChildren;
        }

        public void BeforeWrite(BlockNode control)
        {
            throw new NotImplementedException();
        }
    }
}
