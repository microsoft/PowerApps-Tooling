// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;
public class YamlValidatorLibraryException : Exception
{
    public YamlValidatorLibraryException(string reason) : base(message: reason)
    {
    }
}
