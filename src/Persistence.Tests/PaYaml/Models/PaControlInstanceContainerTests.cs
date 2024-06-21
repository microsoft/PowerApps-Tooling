// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

namespace Persistence.Tests.PaYaml.Models;

[TestClass]
public class PaControlInstanceContainerTests : TestBase
{
    [TestMethod]
    public void DescendantControlInstancesSingleLevel()
    {
        var screen = new NamedObject<ScreenInstance>("Screen1", new()
        {
            Children = [
                new("Ctrl0", new("GroupContainer")),
                new("Ctrl1", new("GroupContainer")),
                new("Ctrl2", new("GroupContainer")),
                ],
        });

        screen.DescendantControlInstances().SelectNames().Should().Equal(new[]
        {
            "Ctrl0",
            "Ctrl1",
            "Ctrl2",
        }, "items should be in document order");
    }

    [TestMethod]
    public void DescendantControlInstances1AtEachLevel()
    {
        var screen = new NamedObject<ScreenInstance>("Screen1", new()
        {
            Children = [
                new("Ctrl0", new("GroupContainer")
                {
                    Children = [
                        new("Ctrl0.0", new("GroupContainer")
                        {
                            Children = [new("Ctrl0.0.0", new("GroupContainer"))]
                        }),
                        ]
                }),
                ],
        });

        screen.DescendantControlInstances().SelectNames().Should().Equal(new[]
        {
            "Ctrl0",
            "Ctrl0.0",
            "Ctrl0.0.0",
        }, "items should be in document order");
    }

    [TestMethod]
    public void DescendantControlInstancesMultiLevelTest()
    {
        var screen = new NamedObject<ScreenInstance>("Screen1", new()
        {
            Children = [
                new("Ctrl0", new("GroupContainer")
                {
                    Children = [
                        new("Ctrl0.0", new("Label")),
                        new("Ctrl0.1", new("Label")),
                        ],
                }),
                new("Ctrl1", new("GroupContainer")
                {
                    Children = [
                        new("Ctrl1.0", new("Label")),
                        new("Ctrl1.1", new("GroupContainer")
                        {
                            Children = [
                                new("Ctrl1.1.0", new("Label")),
                                new("Ctrl1.1.1", new("Label")),
                                ],
                        }),
                        new("Ctrl1.2", new("Label")),
                        ],
                }),
                new("Ctrl2", new("GroupContainer")
                {
                    Children = [
                        new("Ctrl2.0", new("Label")),
                        new("Ctrl2.1", new("Label")),
                        ],
                }),
                ],
        });

        screen.DescendantControlInstances().SelectNames().Should().Equal(new[]
        {
            "Ctrl0",
            "Ctrl0.0",
            "Ctrl0.1",
            "Ctrl1",
            "Ctrl1.0",
            "Ctrl1.1",
            "Ctrl1.1.0",
            "Ctrl1.1.1",
            "Ctrl1.2",
            "Ctrl2",
            "Ctrl2.0",
            "Ctrl2.1",
        }, "items should be in document order");
    }
}
