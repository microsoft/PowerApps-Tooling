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

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Constants related to folders and file names of entries within an *.msapr file
/// </summary>
public static class MsaprLayoutConstants
{
    /// <summary>
    /// Names of directories within an msapr.
    /// </summary>
    public static class DirectoryNames
    {
        /// <summary>
        /// The directory where we put entries that are extracted from the original msapp.
        /// These SHOULD be unmodified from the original msapp.
        /// </summary>
        /// <remarks>
        /// Why do we need this directory?  Why not just put extracted entries at the root of the msapr?<br/>
        /// This helps file entry management in the msapr.  It makes it easy to know which entries came from the original msapp and which are generated during packing.
        /// It allows the msapp stucture to change over time, with simpler forward-compatibility.
        /// </remarks>
        public const string Msapp = "msapp";
    }

    /// <summary>
    /// File names for well-known files in the msapr.
    /// </summary>
    public static class FileNames
    {
        public const string MsaprHeader = "msapr-header.json";
    }

    /// <summary>
    /// These file extensions should be similar to .net's System.IO.Path.GetExtension method behavior.
    /// Namely, they should begin with a dot and be in lower case.
    /// But this doesn't mean they can't be combined file extensions like ".pa.yaml".
    /// </summary>
    public static class FileExtensions
    {
        public const string Msapr = ".msapr";
    }
}
