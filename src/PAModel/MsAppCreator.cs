// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal static class MsAppMaker
    {
        private static readonly string _defaultThemefileName = "Microsoft.PowerPlatform.Formulas.Tools.Themes.DefaultTheme.json";

        public static CanvasDocument Create(string appName, string packagesPath, IList<string> paFiles)
        {
            var app = new CanvasDocument();

            app._properties = DocumentPropertiesJson.CreateDefault(appName);
            app._header = HeaderJson.CreateDefault();

            LoadTemplateFiles(app, packagesPath, out var loadedTemplates);
            app._entropy = new Entropy();
            app._checksum = new ChecksumJson() { ClientStampedChecksum = "Foo" };

            AddDefaultTheme(app);

            CreateControls(app, paFiles, loadedTemplates);

            return app;
        }

        private static void AddDefaultTheme(CanvasDocument app)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(_defaultThemefileName);
            using var reader = new StreamReader(stream);

            var jsonString = reader.ReadToEnd();
            var bytes = Encoding.UTF8.GetBytes(jsonString);

            app.AddFile(new FileEntry { Name = "References\\Themes.json", RawBytes = bytes });
        }

        private static void LoadTemplateFiles(CanvasDocument app, string packagesPath, out Dictionary<string, ControlTemplate> loadedTemplates)
        {
            loadedTemplates = new Dictionary<string, ControlTemplate>();
            var templateList = new List<TemplatesJson.TemplateJson>();
            var directoryReader = new DirectoryReader(packagesPath);
            foreach (var file in directoryReader.EnumerateFiles(string.Empty, "*.xml"))
            {
                var xmlContents = file.GetContents();
                if (!ControlTemplateParser.TryParseTemplate(xmlContents, app._properties.DocumentAppType, out var parsedTemplate, out var templateName))
                    throw new NotSupportedException($"Unable to parse template file {file._relativeName}");
                loadedTemplates.Add(templateName, parsedTemplate);
                templateList.Add(new TemplatesJson.TemplateJson() { Name = templateName, Template = xmlContents, Version = parsedTemplate.Version });
            }

            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(loadedTemplates, app._properties.DocumentAppType);

            app._templates = new TemplatesJson() { UsedTemplates = templateList.ToArray() };
        }

        private static void CreateControls(CanvasDocument app, IList<string> paFiles, Dictionary<string, ControlTemplate> templateDefaults)
        {
            int index = 0;
            foreach (var file in paFiles)
            {
                var filename = Path.GetFileName(file);
                var controlName = filename.Remove(filename.IndexOf(".pa1"));
                var fileEntry = new DirectoryReader.Entry(file);
                try
                {
                    var parser = new Parser.Parser(file, fileEntry.GetContents(), null, null, templateDefaults);
                    var item = parser.ParseControl();
                    if (parser.HasErrors())
                    {
                        parser.WriteErrors();
                        Console.WriteLine("Skipping adding file to .msapp due to parse errors");
                        Console.WriteLine("This tool is still in development, if these errors are wrong, please open an issue on our github page with a copy of your app");

                        continue;
                    }
                    item.ExtensionData["Index"] = index++;

                    var control = new ControlInfoJson() { TopParent = item };

                    var sf = SourceFile.New(control);

                    app._sources.Add(sf.ControlName, sf);
                }
                catch
                {
                    Console.WriteLine(
                        "Parsing failed for file " + filename + "\n" +
                        "This tool is still in development, please open an issue on our github page with a copy of your app");
                }
            }
        }
    }
}
