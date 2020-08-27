using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Reflection;

namespace PAModel
{
    enum SourceKind
    {
        Control,
        UxComponent,
        DataComponent
    }

    class SourceFile
    {
        // the source of truth. 
        public ControlInfoJson Value { get; set; }

        public SourceKind Kind { get; set; }

        // Convenience accessors.
        public string ControlName => this.Value.TopParent.Name;
        public string ControlId => this.Value.TopParent.ControlUniqueId;

        // For a data component, this is a guid. Very important. 
        public string TemplateName => this.Value.TopParent.Template.Name;

        private string GetMsAppFilename()
        {
            if (this.Kind == SourceKind.Control)
            {
                return $"Controls\\{ControlId}.json";
            } else if (this.Kind == SourceKind.DataComponent || this.Kind == SourceKind.UxComponent)
            {
                return $"Components\\{ControlId}.json";
            }
            throw new NotImplementedException($"Unrecognized source kind:" + this.Kind);
        }

        public FileEntry ToMsAppFile()
        {
            var file = MsAppSerializer.ToFile(FileKind.Unknown, this.Value);
            file.Name = this.GetMsAppFilename();

            return file;
        }

        public static SourceFile New(ControlInfoJson json)
        {
            SourceFile sf = new SourceFile();

            sf.Value = json;

            var x = sf.Value.TopParent;
            if (x.Template.Id == ControlInfoJson.Template.DataComponentId)
            {
                sf.Kind = SourceKind.DataComponent;                
            }
            else if (x.Template.Id == ControlInfoJson.Template.UxComponentId)
            {
                sf.Kind = SourceKind.UxComponent;
            } else 
            {
                // UX Control has many different Template ids. 
                sf.Kind = SourceKind.Control;
            }

            return sf;
        }
    }
}