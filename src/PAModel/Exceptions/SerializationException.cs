// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools;

internal class SerializationException : Exception
{
    public string FileName { get; init; }

    public SerializationException(string message)
        : base(message)
    {
    }

    public SerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SerializationException(string message, string fileName, Exception innerException)
    {
        FileName = fileName;
    }
}
