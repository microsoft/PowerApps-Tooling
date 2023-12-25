// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools;

// Internal exception to throw on fatal error to stop document load.
// Caller should have added an error to the error container first.
// These should be caught internally. 
internal class DocumentException : Exception
{
}
