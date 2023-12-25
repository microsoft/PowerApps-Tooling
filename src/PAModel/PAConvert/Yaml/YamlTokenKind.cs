// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

internal enum YamlTokenKind
{
    /// <summary>
    /// A property. Could be 
    /// </summary>
    Property,

    /// <summary>
    /// The start of an object. 
    /// </summary>
    StartObj,

    /// <summary>
    /// End of an object. Matches with StartObj. 
    /// </summary>
    EndObj,

    /// <summary>
    /// End of the file
    /// </summary>
    EndOfFile,

    /// <summary>
    /// Represents a lex error. 
    /// </summary>
    Error
}
