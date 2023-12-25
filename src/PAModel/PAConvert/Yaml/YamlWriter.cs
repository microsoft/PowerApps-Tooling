// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

/// <summary>
/// Helper to write out a safe subset of Yaml.
/// Notably, property values are always either multi-line escaped or
/// prefixed with '=' to block yaml from treating it as a yaml expression.
/// </summary>
public class YamlWriter : IDisposable
{
    private readonly TextWriter _textWriter;
    private int _currentIndent;
    private bool _isDisposed;
    private const string Indent = "    ";

    public YamlWriter(TextWriter text)
    {
        _textWriter = text ?? throw new ArgumentNullException(nameof(text));
    }

    public YamlWriter(Stream stream)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));
        _textWriter = new StreamWriter(stream);
    }

    public void WriteStartObject(string propertyName)
    {
        WriteIndent();

        var needsEscape = propertyName.IndexOfAny(new char[] { '\"', '\'' }) != -1;
        if (needsEscape)
            propertyName = $"\"{propertyName.Replace("\"", "\\\"")}\"";

        _textWriter.Write(propertyName);
        _textWriter.WriteLine(":");

        _currentIndent++;
    }

    public void WriteEndObject()
    {
        _currentIndent--;
        if (_currentIndent < 0)
        {
            throw new InvalidOperationException("No matching start object");
        }
    }

    public void WriteProperty(string propertyName, bool value, bool includeEquals = true)
    {
        WriteIndent();
        _textWriter.Write(propertyName);
        _textWriter.Write(": ");
        if (includeEquals)
        {
            _textWriter.Write("=");
        }
        _textWriter.WriteLine(value ? "true" : "false");
    }

    public void WriteProperty(string propertyName, int value, bool includeEquals = true)
    {
        WriteIndent();
        _textWriter.Write(propertyName);
        _textWriter.Write(": ");
        if (includeEquals)
        {
            _textWriter.Write("=");
        }
        _textWriter.WriteLine(value);
    }

    public void WriteProperty(string propertyName, double value, bool includeEquals = true)
    {
        WriteIndent();
        _textWriter.Write(propertyName);
        _textWriter.Write(": ");
        if (includeEquals)
        {
            _textWriter.Write("=");
        }
        _textWriter.WriteLine(value);
    }

    /// <summary>
    /// Safely write a property. Based on the value, will chose whether single-line (and prefix with an '=')
    /// or multi-line and pick the right the escape. 
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <param name="includeEquals"></param>
    public void WriteProperty(string propertyName, string value, bool includeEquals = true)
    {
        if (value == null)
        {
            WriteIndent();
            _textWriter.Write(propertyName);
            _textWriter.WriteLine(":");
            return;
        }

        value = NormalizeNewlines(value);

        var isSingleLine = value.IndexOfAny(new char[] { '#', '\n', ':' }) == -1;

        // For consistency, both single and multiline PA properties prefix with '='.
        // Only single-line actually needs this - to avoid yaml's regular expression escaping.
        if (includeEquals)
        {
            value = '=' + value;
        }

        if (isSingleLine)
        {
            WriteIndent();
            _textWriter.Write(propertyName);
            _textWriter.Write(": ");
            _textWriter.WriteLine(value);
        }
        else
        {
            WriteIndent();
            _textWriter.Write(propertyName);
            _textWriter.Write(": ");

            var numNewlines = 0;
            for (var i = value.Length - 1; i > 0; i--)
            {
                if (value[i] == '\n')
                {
                    numNewlines++;
                }
                else
                {
                    break;
                }
            }
            switch (numNewlines)
            {
                case 0: _textWriter.WriteLine("|-"); break;
                case 1: _textWriter.WriteLine("|"); break;
                default: _textWriter.WriteLine("|+"); break;
            }

            _currentIndent++;

            var needIndent = true;
            foreach (var ch in value)
            {
                if (needIndent)
                {
                    WriteIndent();
                    needIndent = false;
                }

                if (ch == '\n')
                {
                    _textWriter.WriteLine();
                    needIndent = true;
                    continue;
                }
                _textWriter.Write(ch);
            }

            if (numNewlines == 0)
            {
                _textWriter.WriteLine();
            }

            _currentIndent--;
        }
    }

    // Write a newline, for aesthics. since this is inbetween properties,
    // it should get ignored on parse. 
    public void WriteNewline()
    {
        _textWriter.WriteLine();
    }

    private void WriteIndent()
    {
        for (var i = 0; i < _currentIndent; i++)
        {
            _textWriter.Write(Indent);
        }
    }

    private string NormalizeNewlines(string x)
    {
        return x.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _textWriter?.Dispose();
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
