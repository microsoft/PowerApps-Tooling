// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
using YamlDotNet.Core;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

public class PersistenceException : Exception
{
    public PersistenceException(PersistenceErrorCode errorCode)
        : this(errorCode, null)
    {
    }

    public PersistenceException(PersistenceErrorCode errorCode, Exception? innerException)
        : base(errorCode.GetDefaultExceptionMessage(), innerException) // Use default error message so we don't expose potential user data
    {
        ErrorCode = errorCode.CheckArgumentInRange();
    }

    /// <summary>
    /// The reason for this exception, which MAY contain user personal data or EUPI.
    /// This value is never printed to logging.
    /// </summary>
    public string? Reason { get; init; }

    public PersistenceErrorCode ErrorCode { get; }

    /// <summary>
    /// Returns the string representation of this instance that is safe to write to loggers.
    /// Namely, it excludes data which could possibly contain personal data or PII.
    /// </summary>
    public string LoggerSafeMessage => ComposeMessage(loggerSafeOnly: true);

    private string ComposeMessage(bool loggerSafeOnly)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"{(int)ErrorCode} : ");

        if (!loggerSafeOnly && Reason != null)
        {
            sb.Append(Reason);
        }
        else
        {
            sb.Append(ErrorCode.GetDefaultExceptionMessage());
        }

        return sb.ToString();
    }

    internal static PersistenceException FromYamlException(YamlException ex, PersistenceErrorCode errorCode)
    {
        // PRIVACY: We can't trust that the YamlException.Message doesn't contain user personal data or EUPI, so we must not add it to the innerException
        // But we can add the message to the Reason.
        return new PersistenceException(errorCode) { Reason = ex.Message };
    }
}
