// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    /// <summary>
    /// Represents a PowerApps document.  This can be save/loaded from a MsApp or Source representation. 
    /// This is a full in-memory representation of the msapp file. 
    /// </summary>
    public class CanvasDocument
    {
        // Rules for CanvasDocument
        // - Save/Load must faithfully roundtrip an msapp exactly. 
        // - this is an in-memory representation - so it must parse/shard everything on load. 
        // - Save should not mutate any state. 

        // Track all unknown "files". Ensures round-tripping isn't lossy.         
        // Only contains files of FileKind.Unknown
        internal Dictionary<string, FileEntry> _unknownFiles = new Dictionary<string, FileEntry>();

        // Key is Top Parent Control Name.
        // Includes both Controls and Components, represented as IR
        internal Dictionary<string, BlockNode> _sources = new Dictionary<string, BlockNode>();
        internal EditorStateStore _editorStateStore;
        internal TemplateStore _templateStore;

        // Various data sources        
        // This is references\dataSources.json
        // Also includes entries for DataSources made from a DataComponent
        // private Dictionary<string, DataSourceEntry> _dataSources = new Dictionary<string, DataSourceEntry>();
        // List instead of Dict  since we don't have a unique key. Name can be reused. 
        private List<DataSourceEntry> _dataSources = new List<DataSourceEntry>();

        internal HeaderJson _header;
        internal DocumentPropertiesJson _properties;
        internal PublishInfoJson _publishInfo;
        internal TemplatesJson _templates;
        internal ThemesJson _themes;

        // Environment-specific information
        // Extracted from _properties.LocalConnectionReferences
        // Key is a Connection.Id
        internal IDictionary<string, ConnectionJson> _connections;

        internal FileEntry _logoFile;

        // Save for roundtripping.
        internal Entropy _entropy = new Entropy();

        // checksum from existin msapp. 
        internal ChecksumJson _checksum;

        #region Save/Load

        /// <summary>
        /// Load an .msapp file for a Canvas Document. 
        /// </summary>
        /// <param name="fullPathToMsApp">path to an .msapp file</param>
        /// <returns>A tuple of the document and errors and warnings. If there are errors, the document is null.  </returns>
        public static (CanvasDocument,ErrorContainer) LoadFromMsapp(string fullPathToMsApp)
        {
            var errors = new ErrorContainer();
            var doc = Wrapper(() => MsAppSerializer.Load(fullPathToMsApp, errors), errors);
            return (doc, errors);            
        }

        public static (CanvasDocument,ErrorContainer) LoadFromSources(string pathToSourceDirectory)
        {
            var errors = new ErrorContainer();
            var doc = Wrapper(() => SourceSerializer.LoadFromSource(pathToSourceDirectory, errors), errors);
            return (doc, errors);
        }

        public ErrorContainer SaveToMsApp(string fullPathToMsApp)
        {
            var errors = new ErrorContainer();
            Wrapper(() => MsAppSerializer.SaveAsMsApp(this, fullPathToMsApp, errors), errors);
            return errors;
        }
        public ErrorContainer SaveToSources(string pathToSourceDirectory)
        {
            var errors = new ErrorContainer();
            Wrapper(() => SourceSerializer.SaveAsSource(this, pathToSourceDirectory, errors), errors);
            return errors;
        }
        public static (CanvasDocument, ErrorContainer) MakeFromSources(string appName, string packagesPath, IList<string> paFiles)
        {
            var errors = new ErrorContainer();
            var doc = Wrapper(() => SourceSerializer.Create(appName, packagesPath, paFiles, errors), errors);
            return (doc, errors);
        }

        #endregion

        // Wrapper to ensure consistent invariants between loading a document, exception handling, and returning errors. 
        private static CanvasDocument Wrapper(Func<CanvasDocument> worker, ErrorContainer errors)
        {
            CanvasDocument document = null;
            try
            {
                document = worker();
                if (errors.HasErrors)
                {
                    return null;
                }
                return document;
            }
            catch (Exception e)
            {
                if (!errors.HasErrors)
                {
                    // Internal error - something was thrown without adding to the error container.
                    // Add at least one error
                    errors.InternalError(e);
                }
                return null;
            }
        }

        private static void Wrapper(Action worker, ErrorContainer errors)
        {
            try
            {
                worker();
            }
            catch (Exception e)
            {
                if (!errors.HasErrors)
                {
                    // Internal error - something was thrown without adding to the error container.
                    // Add at least one error
                    errors.InternalError(e);
                }
            }
        }

        internal CanvasDocument()
        {
            _editorStateStore = new EditorStateStore();
            _templateStore = new TemplateStore();
        }

        // iOrder is used to preserve ordering value for round-tripping. 
        internal void AddDataSourceForLoad(DataSourceEntry ds, int? order = null)
        {
            // Don't allow overlaps;
            // Names are not unique. 
            _dataSources.Add(ds);

            _entropy.Add(ds, order);
        }
        internal IEnumerable<DataSourceEntry> GetDataSources()
        {
            return _dataSources;
        }

        internal void ApplyAfterMsAppLoadTransforms()
        {
            // Shard templates, parse for default values
            var templateDefaults = new Dictionary<string, ControlTemplate>();
            foreach (var template in _templates.UsedTemplates)
            {
                if (!ControlTemplateParser.TryParseTemplate(template.Template, _properties.DocumentAppType, templateDefaults, out _, out _))
                    throw new NotSupportedException($"Unable to parse template file {template.Name}");
            }

            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(templateDefaults, _properties.DocumentAppType);

            var componentInstanceTransform = new ComponentInstanceTransform();
            var componentDefTransform = new ComponentDefinitionTransform(_templateStore, componentInstanceTransform);

            // Transform component definitions and populate template set of component instances that need updates 
            foreach (var ctrl in _sources)
            {
                componentDefTransform.AfterRead(ctrl.Value);
            }

            var transformer = new SourceTransformer(templateDefaults, new Theme(_themes), componentInstanceTransform, _editorStateStore, _templateStore);

            foreach (var ctrl in _sources)
            {
                transformer.ApplyAfterRead(ctrl.Value);
            }
        }

        internal void ApplyBeforeMsAppWriteTransforms()
        {
            // Shard templates, parse for default values
            var templateDefaults = new Dictionary<string, ControlTemplate>();
            foreach (var template in _templates.UsedTemplates)
            {
                if (!ControlTemplateParser.TryParseTemplate(template.Template, _properties.DocumentAppType, templateDefaults, out _, out _))
                    throw new NotSupportedException($"Unable to parse template file {template.Name}");
            }

            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(templateDefaults, _properties.DocumentAppType);

            var componentInstanceTransform = new ComponentInstanceTransform();
            var componentDefTransform = new ComponentDefinitionTransform(_templateStore, componentInstanceTransform);

            // Transform component definitions and populate template set of component instances that need updates 
            foreach (var ctrl in _sources)
            {
                componentDefTransform.BeforeWrite(ctrl.Value);
            }

            var transformer = new SourceTransformer(templateDefaults, new Theme(_themes), componentInstanceTransform, _editorStateStore, _templateStore);

            foreach (var ctrl in _sources)
            {
                transformer.ApplyBeforeWrite(ctrl.Value);
            }
        }


        // Called after loading. This will check internal fields and fill in consistency data. 
        internal void OnLoadComplete()
        {
            // Do integrity checks. 
            if (_header == null)
            {
                throw new InvalidOperationException($"Missing header file");
            }
            if (_properties == null)
            {
                throw new InvalidOperationException($"Missing properties file");
            }

            // Integrity checks. 
            // Make sure every connection has a corresponding data source. 
            foreach (var kv in _connections.NullOk())
            {
                var connection = kv.Value;

                if (kv.Key != connection.id)
                {
                    throw new InvalidOperationException($"Document consistency error. Id mismatch");
                }
                foreach (var dataSourceName in connection.dataSources)
                {
                    var ds = _dataSources.Where(x => x.Name == dataSourceName).FirstOrDefault();
                    if (ds == null)
                    {
                        throw new InvalidOperationException($"Document error: Connection '{dataSourceName}' does not have a corresponding data source.");
                    }
                }
            }
        }

        internal static IEnumerable<ControlInfoJson.Item> WalkAll(ControlInfoJson.Item x)
        {
            yield return x;
            if (x.Children != null)
            {
                foreach (var child in x.Children)
                {
                    var subItems = WalkAll(child);
                    foreach (var subItem in subItems)
                    {
                        yield return subItem;
                    }
                }
            }
        }
    }
}
