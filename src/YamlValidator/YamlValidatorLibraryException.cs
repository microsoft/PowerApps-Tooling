// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;
public class YamlValidatorLibraryException : Exception
{
    public string Reason { get; }
    public YamlValidatorLibraryException(string reason)
    {
        Reason = reason;
    }
}
