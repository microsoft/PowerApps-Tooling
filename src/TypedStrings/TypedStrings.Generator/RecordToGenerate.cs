// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.TypedStrings.Generator;

public readonly record struct RecordToGenerate
{
    public readonly string Namespace;
    public readonly string Name;

    public RecordToGenerate(string @namespace, string name)
    {
        Namespace = @namespace;
        Name = name;
    }
}
