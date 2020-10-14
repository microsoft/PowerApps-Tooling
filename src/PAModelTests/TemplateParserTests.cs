// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PAModelTests
{
    [TestClass]
    public class TemplateParserTests
    {
        // This test is validating that the control template parser correctly parses default values for gallery control's nested template
        // This will likely change after the preprocessor logic to combine nested templates with the parent is added.
        [TestMethod]
        public void TestGalleryNestedTemplateParse()
        {
            var galleryTemplatePath = Path.Combine(Environment.CurrentDirectory, "Templates", "gallery_2.10.0.xml");
            Assert.IsTrue(File.Exists(galleryTemplatePath));

            using var galleryTemplateStream = File.OpenRead(galleryTemplatePath);
            using var galleryTemplateReader = new StreamReader(galleryTemplateStream);

            var galleryTemplateContents = galleryTemplateReader.ReadToEnd();

            var parsedTemplates = new Dictionary<string, ControlTemplate>();
            Assert.IsTrue(ControlTemplateParser.TryParseTemplate(galleryTemplateContents, AppType.DesktopOrTablet, parsedTemplates, out var topTemplate, out var name));

            Assert.AreEqual(2, parsedTemplates.Count);
            Assert.AreEqual("gallery", name);
            Assert.AreEqual("http://microsoft.com/appmagic/gallery", topTemplate.Id);

            Assert.IsTrue(parsedTemplates.TryGetValue("galleryTemplate", out var innerTemplate));
            Assert.AreEqual("http://microsoft.com/appmagic/galleryTemplate", innerTemplate.Id);
            Assert.AreEqual("RGBA(0, 0, 0, 0)", innerTemplate.InputDefaults["TemplateFill"]);
        }
    }
}
