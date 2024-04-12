// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

[Serializable]
internal class PaYamlSerializationException : Exception
{
    public PaYamlSerializationException(string reason, PaYamlLocation? eventStart = null)
        : this(reason, eventStart, null)
    {
    }

    public PaYamlSerializationException(string reason, PaYamlLocation? eventStart, Exception? innerException)
        : base(reason, innerException)
    {
        EventStart = eventStart;
    }

    protected PaYamlSerializationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        EventStart = (PaYamlLocation?)info.GetValue(nameof(EventStart), typeof(PaYamlLocation));
    }

    public override string Message => EventStart == null ? Reason : $"{Reason} (Line: {EventStart.Line}, Col: {EventStart.Column})";

    public string Reason => base.Message; // we get the storage of a string for free

    public PaYamlLocation? EventStart { get; }
}
