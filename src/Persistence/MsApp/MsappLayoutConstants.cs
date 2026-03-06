// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Constants related to folders and file names of entries within an *.msapp file
/// </summary>
public static class MsappLayoutConstants
{
    /// <summary>
    /// Names of directories within an msapp.
    /// </summary>
    public static class DirectoryNames
    {
        public const string Assets = "Assets";
        public const string Components = "Components";
        public const string Controls = "Controls";
        public const string References = "References";
        public const string Src = "Src";
    }

    /// <summary>
    /// File names for well-known files in the msapp.
    /// </summary>
    public static class FileNames
    {
        public const string Header = "Header.json";
        public const string Properties = "Properties.json";
        public const string Themes = "Themes.json";
        public const string Resources = "Resources.json";

        /// <summary>
        /// This file is added to the root of the msapp when the app is packed via supported tooling.
        /// It contains information about the packing operation and instructions for how the document server should load the app.
        /// </summary>
        public const string Packed = "packed.json";
    }

    /// <summary>
    /// These file extensions should be similar to .net's System.IO.Path.GetExtension method behavior.
    /// Namely, they should begin with a dot and be in lower case.
    /// But this doesn't mean they can't be combined file extensions like ".pa.yaml".
    /// </summary>
    public static class FileExtensions
    {
        public const string PaYaml = ".pa.yaml";
    }
}
