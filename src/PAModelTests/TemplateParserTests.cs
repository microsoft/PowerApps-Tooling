// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;

namespace PAModelTests;

[TestClass]
public class TemplateParserTests
{
    // This test is validating that the control template parser correctly parses default values for gallery control's nested template
    // This will likely change after the preprocessor logic to combine nested templates with the parent is added.
    [TestMethod]
    public void TestGalleryNestedTemplateParse()
    {
        var galleryTemplatePath = Path.Combine(Environment.CurrentDirectory, "Templates", "gallery_2.10.0.xml");
        File.Exists(galleryTemplatePath).Should().BeTrue();

        using var galleryTemplateStream = File.OpenRead(galleryTemplatePath);
        using var galleryTemplateReader = new StreamReader(galleryTemplateStream);

        var galleryTemplateContents = galleryTemplateReader.ReadToEnd();

        var parsedTemplates = new Dictionary<string, ControlTemplate>();
        var templateStore = new TemplateStore();
        Assert.IsTrue(ControlTemplateParser.TryParseTemplate(templateStore, galleryTemplateContents, AppType.DesktopOrTablet, parsedTemplates, out var topTemplate, out var name));

        Assert.AreEqual(2, parsedTemplates.Count);
        Assert.AreEqual("gallery", name);
        Assert.AreEqual("http://microsoft.com/appmagic/gallery", topTemplate.Id);

        Assert.IsTrue(templateStore.TryGetTemplate("gallery", out _));

        Assert.IsTrue(parsedTemplates.TryGetValue("galleryTemplate", out var innerTemplate));
        Assert.AreEqual("http://microsoft.com/appmagic/galleryTemplate", innerTemplate.Id);
        Assert.AreEqual("RGBA(0, 0, 0, 0)", innerTemplate.InputDefaults["TemplateFill"]);

        Assert.IsTrue(templateStore.TryGetTemplate("galleryTemplate", out _));
    }
}
