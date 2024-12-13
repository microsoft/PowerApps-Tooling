// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.TypedStrings.Generator;

internal readonly record struct RecordToGenerate
{
    public readonly string Namespace;
    public readonly string Name;
    public readonly bool HasIsValidMethod;

    public RecordToGenerate(string @namespace, string name, bool hasIsValidMethod)
    {
        Namespace = @namespace;
        Name = name;
        HasIsValidMethod = hasIsValidMethod;
    }
}
