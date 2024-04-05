// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using YamlDotNet.Core;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml;

[Serializable]
internal class PaYamlSerializationException : Exception
{
    public PaYamlSerializationException(string message, Mark eventStart) : base(message)
    {
        EventStart = eventStart;
    }

    public PaYamlSerializationException(string message, Mark eventStart, Exception? innerException) : base(message, innerException)
    {
        EventStart = eventStart;
    }

    protected PaYamlSerializationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public Mark EventStart { get; }
}
