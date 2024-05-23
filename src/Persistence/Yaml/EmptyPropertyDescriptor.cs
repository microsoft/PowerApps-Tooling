// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class EmptyPropertyDescriptor(string name) : IPropertyDescriptor
{
    public string Name { get; } = name;

    public Type Type => typeof(object);

    public Type? TypeOverride { get; set; }

    public int Order { get; set; }

    public ScalarStyle ScalarStyle { get; set; }

    public bool CanWrite => true;

    public void Write(object? target, object? value)
    {
    }

    public T? GetCustomAttribute<T>() where T : Attribute
    {
        return null;
    }

    public IObjectDescriptor Read(object? target)
    {
        return new ObjectDescriptor(target, typeof(object), typeof(object));
    }
}
