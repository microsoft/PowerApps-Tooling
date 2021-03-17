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
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;

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
        internal Dictionary<FilePath, FileEntry> _unknownFiles = new Dictionary<FilePath, FileEntry>();

        // Key is Top Parent Control Name for both _screens and _components
        internal Dictionary<string, BlockNode> _screens = new Dictionary<string, BlockNode>(StringComparer.Ordinal);
        internal Dictionary<string, BlockNode> _components = new Dictionary<string, BlockNode>(StringComparer.Ordinal);

        internal EditorStateStore _editorStateStore;
        internal TemplateStore _templateStore;

        // Various data sources        
        // This is references\dataSources.json
        // Also includes entries for DataSources made from a DataComponent
        // Key is parent entity name (datasource name for non cds data sources)
        private Dictionary<string, List<DataSourceEntry>> _dataSources = new Dictionary<string, List<DataSourceEntry>>(StringComparer.Ordinal);
        internal List<string> _screenOrder = new List<string>();


        internal HeaderJson _header;
        internal DocumentPropertiesJson _properties;
        internal PublishInfoJson _publishInfo;
        internal TemplatesJson _templates;
        internal ThemesJson _themes;
        internal ResourcesJson _resourcesJson;
        internal AppCheckerResultJson _appCheckerResultJson;

        // Environment-specific information
        // Extracted from _properties.LocalConnectionReferences
        // Key is a Connection.Id
        internal IDictionary<string, ConnectionJson> _connections;

        // Extracted from _properties.LocalDatasourceReferences
        // Key is a dataset name
        internal IDictionary<string, LocalDatabaseReferenceJson> _dataSourceReferences;

        // Extracted from _properties.LibraryDependencies
        // Must preserve server ordering. 
        internal ComponentDependencyInfo[] _libraryReferences;

        internal FileEntry _logoFile;

        // Save for roundtripping.
        internal Entropy _entropy = new Entropy();

        // checksum from existing msapp. 
        internal ChecksumJson _checksum;

        // Track all asset files, key is file name
        internal Dictionary<FilePath, FileEntry> _assetFiles = new Dictionary<FilePath, FileEntry>();


        #region Save/Load

        /// <summary>
        /// Load an .msapp file for a Canvas Document. 
        /// </summary>
        /// <param name="fullPathToMsApp">path to an .msapp file</param>
        /// <returns>A tuple of the document and errors and warnings. If there are errors, the document is null.  </returns>
        public static (CanvasDocument, ErrorContainer) LoadFromMsapp(string fullPathToMsApp)
        {
            var errors = new ErrorContainer();

            Utilities.EnsurePathRooted(fullPathToMsApp);

            if (!fullPathToMsApp.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only works for .msapp files");
            }

            using (var stream = new FileStream(fullPathToMsApp, FileMode.Open))
            {
                var doc = Wrapper(() => MsAppSerializer.Load(stream, errors), errors);
                return (doc, errors);
            }
        }

        public static (CanvasDocument, ErrorContainer) LoadFromMsapp(Stream streamToMsapp)
        {
            var errors = new ErrorContainer();
            var doc = Wrapper(() => MsAppSerializer.Load(streamToMsapp, errors), errors);
            return (doc, errors);
        }

        public static (CanvasDocument,ErrorContainer) LoadFromSources(string pathToSourceDirectory)
        {
            Utilities.EnsurePathRooted(pathToSourceDirectory);

            var errors = new ErrorContainer();
            var doc = Wrapper(() => SourceSerializer.LoadFromSource(pathToSourceDirectory, errors), errors);
            return (doc, errors);
        }

        public ErrorContainer SaveToMsApp(string fullPathToMsApp)
        {
            Utilities.EnsurePathRooted(fullPathToMsApp);

            var errors = new ErrorContainer();
            Wrapper(() => MsAppSerializer.SaveAsMsApp(this, fullPathToMsApp, errors), errors);
            return errors;
        }

        // Used to validate roundtrip after unpack
        internal ErrorContainer SaveToMsAppValidation(string fullPathToMsApp)
        {
            Utilities.EnsurePathRooted(fullPathToMsApp);

            var errors = new ErrorContainer();
            Wrapper(() => MsAppSerializer.SaveAsMsApp(this, fullPathToMsApp, errors, isValidation: true), errors);
            return errors;
        }
        public ErrorContainer SaveToSources(string pathToSourceDirectory)
        {
            Utilities.EnsurePathRooted(pathToSourceDirectory);

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

        internal CanvasDocument(CanvasDocument other)
        {
            foreach (var kvp in other._unknownFiles)
            {
                _unknownFiles.Add(kvp.Key, new FileEntry(kvp.Value));
            }

            foreach (var kvp in other._assetFiles)
            {
                _assetFiles.Add(kvp.Key, new FileEntry(kvp.Value));
            }

            foreach (var kvp in other._screens)
            {
                _screens.Add(kvp.Key, kvp.Value.Clone());
            }

            foreach (var kvp in other._components)
            {
                _components.Add(kvp.Key, kvp.Value.Clone());
            }

            _editorStateStore = new EditorStateStore(other._editorStateStore);
            _templateStore = new TemplateStore(other._templateStore);

            _dataSources = other._dataSources.JsonClone();
            _screenOrder = new List<string>(other._screenOrder);

            _header = other._header.JsonClone();
            _properties = other._properties.JsonClone();
            _publishInfo = other._publishInfo.JsonClone();
            _templates = other._templates.JsonClone();
            _themes = other._themes.JsonClone();
            _resourcesJson = other._resourcesJson.JsonClone();
            _appCheckerResultJson = other._appCheckerResultJson.JsonClone();

            _connections = other._connections.JsonClone();

            _dataSourceReferences = other._dataSourceReferences.JsonClone();
            _libraryReferences = other._libraryReferences.JsonClone();

            _logoFile = new FileEntry(other._logoFile);
            _entropy = other._entropy.JsonClone();
            _checksum = other._checksum.JsonClone();
        }

        // iOrder is used to preserve ordering value for round-tripping. 
        internal void AddDataSourceForLoad(DataSourceEntry ds, int? order = null)
        {
            // Key is parent entity name
            var key = ds.RelatedEntityName ?? ds.Name;
            List<DataSourceEntry> list;
            if (!_dataSources.TryGetValue(key, out list))
            {
                list = new List<DataSourceEntry>();
                _dataSources.Add(key, list);
            }

            list.Add(ds);
            _entropy.Add(ds, order);
        }

        // Key is parent entity name
        internal IEnumerable<KeyValuePair<string, List<DataSourceEntry>>> GetDataSources()
        {
            return _dataSources;
        }

        internal void ApplyAfterMsAppLoadTransforms(ErrorContainer errors)
        {
            // Update volatile documentproperties
            _entropy.SetProperties(_properties);

            // Shard templates, parse for default values
            var templateDefaults = new Dictionary<string, ControlTemplate>();
            foreach (var template in _templates.UsedTemplates)
            {
                if (!ControlTemplateParser.TryParseTemplate(_templateStore, template.Template, _properties.DocumentAppType, templateDefaults, out _, out _))
                {
                    errors.GenericError($"Unable to parse template file {template.Name}");
                    throw new DocumentException();
                }
            }

            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(_templateStore, templateDefaults, _properties.DocumentAppType);

            var componentInstanceTransform = new ComponentInstanceTransform(errors);
            var componentDefTransform = new ComponentDefinitionTransform(errors, _templateStore, componentInstanceTransform);

            // Transform component definitions and populate template set of component instances that need updates 
            foreach (var ctrl in _components)
            {
                AddComponentDefaults(ctrl.Value, templateDefaults);
                componentDefTransform.AfterRead(ctrl.Value);
            }

            var transformer = new SourceTransformer(errors, templateDefaults, new Theme(_themes), componentInstanceTransform, _editorStateStore, _templateStore, _entropy);

            foreach (var ctrl in _screens.Concat(_components))
            {
                transformer.ApplyAfterRead(ctrl.Value);
            }
        }

        internal void ApplyBeforeMsAppWriteTransforms(ErrorContainer errors)
        {
            // Update volatile documentproperties
            _entropy.GetProperties(_properties);

            // Shard templates, parse for default values
            var templateDefaults = new Dictionary<string, ControlTemplate>();
            foreach (var template in _templates.UsedTemplates)
            {
                if (!ControlTemplateParser.TryParseTemplate(_templateStore, template.Template, _properties.DocumentAppType, templateDefaults, out _, out _))
                {
                    errors.GenericError($"Unable to parse template file {template.Name}");
                    throw new DocumentException();
                }
            }

            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(_templateStore, templateDefaults, _properties.DocumentAppType);

            var componentInstanceTransform = new ComponentInstanceTransform(errors);
            var componentDefTransform = new ComponentDefinitionTransform(errors, _templateStore, componentInstanceTransform);

            // Transform component definitions and populate template set of component instances that need updates 
            foreach (var ctrl in _components)
            {
                componentDefTransform.BeforeWrite(ctrl.Value);
                AddComponentDefaults(ctrl.Value, templateDefaults);
            }

            var transformer = new SourceTransformer(errors, templateDefaults, new Theme(_themes), componentInstanceTransform, _editorStateStore, _templateStore, _entropy);

            foreach (var ctrl in _screens.Concat(_components))
            {
                transformer.ApplyBeforeWrite(ctrl.Value);
            }
        }

        private void AddComponentDefaults(BlockNode topParent, Dictionary<string, ControlTemplate> templateDefaults)
        {
            var type = topParent.Name.Kind.TypeName;
            if (!_templateStore.TryGetTemplate(type, out var template) || !(template.IsComponentTemplate ?? false))
                return;

            var componentTemplate = new ControlTemplate(type, "", "");

            foreach (var prop in topParent.Properties)
            {
                componentTemplate.InputDefaults.Add(prop.Identifier, prop.Expression.Expression);
            }
            templateDefaults.Add(type, componentTemplate);
        }


        // Called after loading. This will check internal fields and fill in consistency data. 
        internal void OnLoadComplete(ErrorContainer errors)
        {
            // Do integrity checks. 
            if (_header == null)
            {
                errors.FormatNotSupported("Missing header file");
                throw new DocumentException();
            }
            if (_properties == null)
            {
                errors.FormatNotSupported("Missing properties file");
                throw new DocumentException();
            }

            // Will visit all controls and add errors
            var uniqueVisitor = new UniqueControlNameVistor(errors);
            foreach (var control in _screens.Values.Concat(_components.Values))
            {
                uniqueVisitor.Visit(control);
            }


            // Integrity checks. 
            // Make sure every connection has a corresponding data source. 
            foreach (var kv in _connections.NullOk())
            {
                var connection = kv.Value;

                if (kv.Key != connection.id)
                {
                    errors.FormatNotSupported($"Document consistency error. Connection id mismatch");
                    throw new DocumentException();
                }
                foreach (var dataSourceName in connection.dataSources)
                {
                    var ds = _dataSources.SelectMany(x => x.Value).Where(x => x.Name == dataSourceName).FirstOrDefault();
                    if (ds == null)
                    {
                        errors.ValidationError($"Connection '{dataSourceName}' does not have a corresponding data source.");
                        throw new DocumentException();
                    }
                }
            }
        }

        // Get ComponentIds for components we've imported. 
        internal HashSet<string> GetImportedComponents()
        {
            var set = new HashSet<string>();
            if (this._libraryReferences != null)
            {
                foreach(var item in this._libraryReferences)
                {
                    set.Add(item.OriginalComponentDefinitionTemplateId);
                }
            }
            return set;
        }

        // Helper for traversing and ensuring unique control names. 
        class UniqueControlNameVistor
        {
            // Control names are case sensitive. 
            private readonly Dictionary<string, SourceLocation?> _names = new Dictionary<string, SourceLocation?>(StringComparer.Ordinal);
            private readonly ErrorContainer _errors;

            public UniqueControlNameVistor(ErrorContainer errors)
            {
                _errors = errors;
            }

            public void Visit(BlockNode node)
            {
                // Ignore test templates here. 
                // Test templates have control-like syntax, but allowed to repeat names:
                //    Step4 As TestStep:
                if (AppTestTransform.IsTestSuite(node.Name.Kind.TypeName))
                {
                    return;
                }

                this.Visit(node.Name);
                foreach(var child in node.Children )
                {
                    this.Visit(child);
                }
            }

            public void Visit(TypedNameNode node)
            {
                SourceLocation? existing;
                if (_names.TryGetValue(node.Identifier, out existing))
                {
                    _errors.DuplicateSymbolError(node.SourceSpan.GetValueOrDefault(), node.Identifier, existing.GetValueOrDefault());
                }
                else
                {
                    _names.Add(node.Identifier, node.SourceSpan);
                }
            }            
        }
    }
}
