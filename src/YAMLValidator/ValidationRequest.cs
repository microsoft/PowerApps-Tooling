// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal readonly record struct ValidationRequest(string FilePath, string SchemaPath, string FilePathType);
