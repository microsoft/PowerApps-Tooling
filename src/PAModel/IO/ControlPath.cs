// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.IO;

/// <summary>
/// This represents a path to a control in a control tree
/// Each segment is a control name
/// </summary>
[DebuggerDisplay("{string.Join('.', _segments)}")]
internal class ControlPath(IEnumerable<string> segments)
{
    public static ControlPath Empty => new([]);

    // switch this to be a queue?
    private readonly ImmutableArray<string> _segments = [.. segments];

    public string Current => _segments.Length != 0 ? _segments[0] : null;

    public ControlPath Next()
    {
        var newSegments = new List<string>();
        for (var i = 1; i < _segments.Length; ++i)
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
            other._segments.Length == _segments.Length &&
            _segments.SequenceEqual(other._segments);
    }

    public override int GetHashCode()
    {
        return _segments.GetHashCode();
    }
}
