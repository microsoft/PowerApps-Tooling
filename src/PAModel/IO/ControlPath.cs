// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.IO;

/// <summary>
/// This represents a path to a control in a control tree
/// Each segment is a control name
/// </summary>
[DebuggerDisplay("{string.Join('.', _segments)}")]
internal class ControlPath(List<string> segments)
{
    // switch this to be a queue?
    private readonly List<string> _segments = segments;
    public string Current => _segments.Any() ? _segments[0] : null;
    public static ControlPath Empty => new(new List<string>());

    public ControlPath Next()
    {
        var newSegments = new List<string>();
        for (var i = 1; i < _segments.Count; ++i)
        {
            newSegments.Add(_segments[i]);
        }
        return new ControlPath(newSegments);
    }

    public ControlPath Append(string controlName)
    {
        var newPath = new List<string>(_segments)
        {
            controlName
        };
        return new ControlPath(newPath);
    }

    public static bool operator ==(ControlPath left, ControlPath right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(ControlPath left, ControlPath right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj)
    {
        return obj is ControlPath other &&
            other._segments.Count == _segments.Count &&
            _segments.SequenceEqual(other._segments);
    }

    public override int GetHashCode()
    {
        return _segments.GetHashCode();
    }
}
