// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

/// <summary>
/// Deserializer from Yaml 
/// </summary>
public class YamlPocoDeserializer : IDisposable
{
    private YamlLexer _yamlLexer;
    private bool _isDisposed;

    public YamlPocoDeserializer()
    {
    }

    public YamlPocoDeserializer(Stream stream)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));
        _yamlLexer = new YamlLexer(new StreamReader(stream));
    }

    public YamlPocoDeserializer(TextReader reader)
    {
        _ = reader ?? throw new ArgumentNullException(nameof(reader));
        _yamlLexer = new YamlLexer(reader);
    }

    public YamlLexerOptions Options
    {
        get => _yamlLexer.Options;
        set => _yamlLexer.Options = value;
    }

    public T Deserialize<T>(string name = null)
    {
        var result = Activator.CreateInstance<T>();

        // Read the object name.
        var objType = typeof(T);
        var yamlObject = objType.GetCustomAttribute<YamlObjectAttribute>() ?? new YamlObjectAttribute() { Name = objType.Name };
        if (string.IsNullOrWhiteSpace(yamlObject.Name))
            yamlObject.Name = objType.Name;
        if (!string.IsNullOrWhiteSpace(name))
            yamlObject.Name = name;

        // Build dictionary of expected properties that have the YamlProperty attribute.
        var yamlProps = new Dictionary<string, (PropertyInfo info, YamlPropertyAttribute attr)>();
        foreach (var prop in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(prop => Attribute.IsDefined(prop, typeof(YamlPropertyAttribute))))
        {
            var yamlProperty = prop.GetCustomAttribute<YamlPropertyAttribute>();
            if (string.IsNullOrWhiteSpace(yamlProperty.Name))
                yamlProperty.Name = prop.Name;

            if (yamlProps.ContainsKey(yamlProperty.Name))
                throw new YamlParseException($"Duplicate YAML property name '{yamlProperty.Name}' on property '{prop.Name}'");
            yamlProps.Add(yamlProperty.Name, (prop, yamlProperty));
        }

        // Stream must start with expected object.
        YamlToken token;
        token = _yamlLexer.ReadNext();
        if (token.Kind != YamlTokenKind.StartObj)
        {
            if (token.Kind == YamlTokenKind.Error)
                throw new YamlParseException(token.Value, _yamlLexer.CurrentLine);
            else
                throw new YamlParseException($"Expected '{YamlTokenKind.StartObj}', found '{token.Kind}'", _yamlLexer.CurrentLine);
        }
        if (!token.Property.Equals(yamlObject.Name))
            throw new YamlParseException($"Expected '{yamlObject.Name}', found '{token.Property}'", _yamlLexer.CurrentLine);

        // Parse the stream.
        while ((token = _yamlLexer.ReadNext()) != YamlToken.EndObj)
        {
            if (token.Kind == YamlTokenKind.Error)
                throw new YamlParseException(token.Value, _yamlLexer.CurrentLine);

            if (token.Kind == YamlTokenKind.Property)
            {
                if (yamlProps.TryGetValue(token.Property, out var propInfo))
                {
                    try
                    {
                        var typedValue = Convert.ChangeType(token.Value, propInfo.info.PropertyType);
                        propInfo.info.SetValue(result, typedValue);
                    }
                    catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                    {
                        throw new YamlParseException($"Error parsing property '{token.Property}'", _yamlLexer.CurrentLine, ex);
                    }
                }
            }
        }

        return result;
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _yamlLexer?.Dispose();
                _yamlLexer = null;
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
