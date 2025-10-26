// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl;

namespace PAModelTests;

[TestClass]
public class SmartMergeTests
{
    private delegate void BranchChange(CanvasDocument canvasDoc);
    private delegate void ResultValidator(CanvasDocument canvasDoc);

    private static void MergeTester(CanvasDocument baseDoc, BranchChange branchAChange, BranchChange branchBChange, ResultValidator resultValidator)
    {
        var branchADoc = new CanvasDocument(baseDoc);
        var branchBDoc = new CanvasDocument(baseDoc);

        branchAChange(branchADoc);
        branchBChange(branchBDoc);

        var mergeResult = CanvasMerger.Merge(branchADoc, branchBDoc, baseDoc);

        resultValidator(mergeResult);
    }



    [TestMethod]
    public void NoOpMergeTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            // Nothing
        },
        (branchBDoc) =>
        {
            // Nothing
        },
        (resultDoc) =>
        {
            Assert.AreEqual(4, resultDoc._editorStateStore.Contents.Count());
        });
    }

    [TestMethod]
    public void SimpleControlAddTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            branchADoc._screens.TryGetValue("Screen1", out var control);
            control.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Foo",
                    Kind = new TypeNode() { TypeName = "label" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
            });
        },
        (branchBDoc) =>
        {
            branchBDoc._screens.TryGetValue("Screen1", out var control);
            control.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Bar",
                    Kind = new TypeNode() { TypeName = "label" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeOtherProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
            });

        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen1", out var control);
            Assert.IsTrue(control.Children.Any(item => item.Name.Identifier == "Foo"));
            Assert.IsTrue(control.Children.Any(item => item.Name.Identifier == "Bar"));
        });
    }

    [TestMethod]
    public void ControlCollisionTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            branchADoc._screens.TryGetValue("Screen1", out var control);
            control.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Foo",
                    Kind = new TypeNode() { TypeName = "label" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
            });
        },
        (branchBDoc) =>
        {
            branchBDoc._screens.TryGetValue("Screen1", out var control);
            control.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Foo",
                    Kind = new TypeNode() { TypeName = "label" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeOtherProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
            });

        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen1", out var control);
            Assert.AreEqual(1, control.Children.Count(item => item.Name.Identifier == "Foo"));
        });
    }

    [TestMethod]
    public void SimplePropertyEditTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            // Nothing
        },
        (branchBDoc) =>
        {
            branchBDoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            var textProp = label.Properties.First(prop => prop.Identifier == "Text");
            textProp.Expression.Expression = "UpdatedBranchB";
        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            var textProp = label.Properties.First(prop => prop.Identifier == "Text");

            Assert.AreEqual("UpdatedBranchB", textProp.Expression.Expression);
        });
    }

    [TestMethod]
    public void ScreenDeletionTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "Chess_for_Power_Apps_v1.03.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            // Nothing
        },
        (branchBDoc) =>
        {
            branchBDoc._screens.Remove("Home Screen", out var control);
        },
        (resultDoc) =>
        {
            Assert.IsFalse(resultDoc._screens.ContainsKey("Home Screen"));
        });
    }

    [TestMethod]
    public void DefaultPropertyEditTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            // Nothing
        },
        (branchBDoc) =>
        {
            branchBDoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            label.Properties.Add(new PropertyNode() { Identifier = "Fill", Expression = new ExpressionNode() { Expression = "Color.Blue" } });
        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            var fillProp = label.Properties.First(prop => prop.Identifier == "Fill");

            Assert.AreEqual("Color.Blue", fillProp.Expression.Expression);
        });
    }

    [TestMethod]
    public void PropertyEditCollisonTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            branchADoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            var textProp = label.Properties.First(prop => prop.Identifier == "Text");
            textProp.Expression.Expression = "UpdatedBranchA";
        },
        (branchBDoc) =>
        {
            branchBDoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            var textProp = label.Properties.First(prop => prop.Identifier == "Text");
            textProp.Expression.Expression = "UpdatedBranchB";
        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            var textProp = label.Properties.First(prop => prop.Identifier == "Text");

            Assert.AreEqual("UpdatedBranchA", textProp.Expression.Expression);
        });
    }

    [TestMethod]
    public void PropertyRemoveDefaultTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            // Nothing
        },
        (branchBDoc) =>
        {
            branchBDoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            var textProp = label.Properties.First(prop => prop.Identifier == "Text");
            label.Properties.Remove(textProp);
        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen1", out var control);
            var label = control.Children.First(child => child.Name.Identifier == "Label1");
            var textProp = label.Properties.FirstOrDefault(prop => prop.Identifier == "Text");

            Assert.AreEqual(default, textProp);
        });
    }

    [TestMethod]
    public void ScreenAddTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            branchADoc._screens.Add("Screen32", new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Screen32",
                    Kind = new TypeNode() { TypeName = "screen" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
            });
        },
        (branchBDoc) =>
        {
        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen32", out var control);
            Assert.AreEqual(1, control.Properties.Count(item => item.Identifier == "SomeProp"));
            Assert.IsTrue(resultDoc._screenOrder.Contains("Screen32"));
        });
    }

    [TestMethod]
    public void ScreenAddWithChildCollisionTestInLocal()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            var newScreen = new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Screen32",
                    Kind = new TypeNode() { TypeName = "screen" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
            };

            newScreen.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Foo",
                    Kind = new TypeNode() { TypeName = "label" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeOtherProp", Expression = new ExpressionNode() { Expression = "FromA" } } }
            });

            branchADoc._screens.Add("Screen32", newScreen);
            branchADoc._editorStateStore.TryAddControl(new ControlState() { Name = "Screen32", TopParentName = "Screen32" });
            branchADoc._editorStateStore.TryAddControl(new ControlState() { Name = "Foo", TopParentName = "Screen32" });
        },
        (branchBDoc) =>
        {
            branchBDoc._screens.TryGetValue("Screen1", out var control);
            control.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Foo",
                    Kind = new TypeNode() { TypeName = "label" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeOtherProp", Expression = new ExpressionNode() { Expression = "FromB" } } }
            });
            branchBDoc._editorStateStore.TryAddControl(new ControlState() { Name = "Foo", TopParentName = "Screen1" });
        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen32", out var control);
            Assert.AreEqual(1, control.Children.Count());
            resultDoc._screens.TryGetValue("Screen1", out control);
            Assert.IsFalse(control.Children.Any(child => child.Name.Identifier == "Foo"));
        });
    }

    [TestMethod]
    public void ScreenAddWithChildCollisionTestInRemote()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            branchADoc._screens.TryGetValue("Screen1", out var control);
            control.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Foo",
                    Kind = new TypeNode() { TypeName = "label" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeOtherProp", Expression = new ExpressionNode() { Expression = "FromB" } } }
            });
            branchADoc._editorStateStore.TryAddControl(new ControlState() { Name = "Foo", TopParentName = "Screen1" });
        },
        (branchBDoc) =>
        {
            var newScreen = new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Screen32",
                    Kind = new TypeNode() { TypeName = "screen" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
            };

            newScreen.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Foo",
                    Kind = new TypeNode() { TypeName = "label" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeOtherProp", Expression = new ExpressionNode() { Expression = "FromA" } } }
            });

            branchBDoc._screens.Add("Screen32", newScreen);
            branchBDoc._editorStateStore.TryAddControl(new ControlState() { Name = "Screen32", TopParentName = "Screen32" });
            branchBDoc._editorStateStore.TryAddControl(new ControlState() { Name = "Foo", TopParentName = "Screen32" });
        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen32", out var control);
            Assert.AreEqual(0, control.Children.Count());
            resultDoc._screens.TryGetValue("Screen1", out control);
            Assert.IsTrue(control.Children.Any(child => child.Name.Identifier == "Foo"));
        });
    }

    [TestMethod]
    public void AddedPCFTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            branchADoc._screens.TryGetValue("Screen1", out var control);
            control.Children.Add(new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = "Foo",
                    Kind = new TypeNode() { TypeName = "PCFTemplate" }
                },
                Properties = new List<PropertyNode>() { new() { Identifier = "SomeProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
            });

            // These are mocks, feel free to improve if needed to make this test more accurate
            branchADoc._templateStore.AddTemplate("PCFTemplate", new CombinedTemplateState() { Name = "PCFTemplate", IsPcfControl = true });
            branchADoc._pcfControls.Add("PCFTemplate", new PcfControl() { Name = "PCFTemplate" });
        },
        (branchBDoc) =>
        {
        },
        (resultDoc) =>
        {
            resultDoc._screens.TryGetValue("Screen1", out var control);
            Assert.AreEqual(1, control.Children.Count(item => item.Name.Identifier == "Foo"));
            Assert.IsTrue(resultDoc._templateStore.TryGetTemplate("PCFTemplate", out _));
            Assert.IsTrue(resultDoc._pcfControls.ContainsKey("PCFTemplate"));
        });
    }

    [TestMethod]
    public void PropertiesTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            branchADoc._properties.DocumentLayoutWidth = 1111;
            branchADoc._properties.ExtensionData.Add("Foo", branchADoc._properties.ExtensionData.Values.First());
        },
        (branchBDoc) =>
        {
            branchBDoc._properties.DocumentLayoutWidth = 2222;
            branchBDoc._properties.AppPreviewFlagsKey = branchBDoc._properties.AppPreviewFlagsKey.Concat(new List<string>() { "NewFlag" }).ToArray();
        },
        (resultDoc) =>
        {
            Assert.AreEqual(1111, resultDoc._properties.DocumentLayoutWidth);
            Assert.AreEqual(msapp._properties.ExtensionData.Values.First().ToString(), resultDoc._properties.ExtensionData["Foo"].ToString());
            Assert.IsTrue(resultDoc._properties.AppPreviewFlagsKey.Contains("NewFlag"));
        });
    }



    [TestMethod]
    public void ScreenOrderTest()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        Assert.IsFalse(errors.HasErrors);
        msapp._screenOrder = new List<string>() { "A", "B", "C" };

        MergeTester(
        msapp,
        (branchADoc) =>
        {
            branchADoc._screenOrder = new List<string>() { "B", "C", "A" };
        },
        (branchBDoc) =>
        {
            branchBDoc._screenOrder = new List<string>() { "C", "A", "B" };
        },
        (resultDoc) =>
        {
            Assert.IsTrue(new List<string>() { "B", "C", "A" }.SequenceEqual(resultDoc._screenOrder));
        });
    }
}
