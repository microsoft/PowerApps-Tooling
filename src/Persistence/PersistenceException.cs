// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

// TODO: Make this a base exception for all persistence exceptions and remove custom properties.
// TODO: Move location context properties to a derived exception class. e.g. PersistenceSerializationException
public class PersistenceException : Exception
{
    public required string FileName { get; init; }

    public int Line { get; init; }

    public int Column { get; init; }

    public PersistenceException(string message)
        : base(message)
    {
    }

    public PersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
