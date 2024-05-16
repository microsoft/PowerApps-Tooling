// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

public record YamlLocation(int Line, int Column)
{
    internal static YamlLocation? FromMark(Mark mark)
    {
        if (mark.Equals(Mark.Empty))
        {
            return null;
        }

        return new(mark.Line, mark.Column);
    }
}
