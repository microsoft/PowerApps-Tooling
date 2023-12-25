// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools;

internal static class PAConstants
{
    public const char IdentifierDelimiter = '\'';

    public const string PropertyDelimiterToken = "=";
    public const string ControlTemplateSeparator = ":";
    public const string ControlVariantSeparator = ",";
    public const string ControlKeyword = "control";

    public const string ThisPropertyIdentifier = "ThisProperty";

    public const string Header = "//! PAFile:0.1";

    // this is used by the dynamically imported controls like PCF.
    public const string DynamicControlDefinitionJson = "DynamicControlDefinitionJson";
}
