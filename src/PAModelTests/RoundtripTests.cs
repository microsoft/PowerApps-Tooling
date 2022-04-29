// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms;
using System.Collections.Generic;

namespace PAModelTests
{
    // Test that a series of .msapps can succeesfully roundtrip.
    [TestClass]
    public class RoundtripTests
    {
        // Apps live in the "Apps" folder, and should have a build action of "Copy to output"
        [DataTestMethod]
        [DataRow("MyWeather.msapp")]
        [DataRow("Chess_for_Power_Apps_v1.03.msapp")]
        [DataRow("AppWithLabel.msapp")]
        [DataRow("GalleryTestApp.msapp")]
        [DataRow("AccountPlanReviewerMaster.msapp")]
        [DataRow("Marc2PowerPlatformDevOpsAlm.msapp")]
        [DataRow("SimpleScopeVariables.msapp")]
        [DataRow("WadlConnector.msapp")]
        [DataRow("GroupControlTest.msapp")]
        [DataRow("EmptyTestCase.msapp")]
        [DataRow("ComponentTest.msapp")]
        [DataRow("autolayouttest.msapp")]
        [DataRow("TestStudio_Test.msapp")]
        [DataRow("ComponentDefinitionWithAllowGlobalAccessProperty.msapp")]
        [DataRow("ComponentDefinitionWithoutAllowGlobalAccessProperty.msapp")]
        [DataRow("DuplicateScreen.msapp")]
        [DataRow("ComponentNameCollision.msapp")]
        [DataRow("LocaleBasedApplication.msapp")]
        public void StressTestApps(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);

            Assert.IsTrue(File.Exists(root));

            bool ok = MsAppTest.StressTest(root);
            Assert.IsTrue(ok);

            var cloneOk = MsAppTest.TestClone(root);
            // If this fails, to debug it, rerun and set a breakpoint in DebugChecksum().
            Assert.IsTrue(cloneOk, $"Clone failed: " + filename);
        }

        [TestMethod]
        public void AfterReadNullChildren()
        {
            BlockNode control = null;

            control.Children.Add(new BlockNode()
                {
                    Name = new TypedNameNode()
                    {
                        Identifier = "ID"
                    },
                    Properties = new List<PropertyNode>() { new PropertyNode { Identifier = "PropID", Expression = new ExpressionNode() { Expression = "E" } } }
                });

            GalleryTemplateTransform.AfterRead(control);

        }
    }
}
