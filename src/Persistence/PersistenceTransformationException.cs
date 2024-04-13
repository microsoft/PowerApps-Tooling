// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

[Serializable]
internal class PersistenceTransformationException : Exception
{
    public PersistenceTransformationException(string message)
        : base(message)
    {
    }

    public PersistenceTransformationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected PersistenceTransformationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
