// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using YamlDotNet.Core;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

[Serializable]
public class PaPersistenceException : Exception
{
    public PaPersistenceException(PersistenceErrorCode errorCode)
        : this(errorCode, null, null)
    {
    }

    public PaPersistenceException(PersistenceErrorCode errorCode, string reason)
        : this(errorCode, reason, null)
    {
    }

    public PaPersistenceException(PersistenceErrorCode errorCode, Exception innerException)
        : this(errorCode, null, innerException)
    {
    }

    public PaPersistenceException(PersistenceErrorCode errorCode, string? reason, Exception? innerException)
        : base(reason ?? string.Empty, innerException) // Convert reason to non-null so base.Message doesn't get set to the default ex message.
    {
        ErrorCode = errorCode.CheckArgumentInRange();
    }

    protected PaPersistenceException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        ErrorCode = ((PersistenceErrorCode)info.GetInt32(nameof(ErrorCode))).CheckArgumentInRange();
        MsappEntryFullPath = info.GetString(nameof(MsappEntryFullPath));
        JsonPath = info.GetString(nameof(JsonPath));

        // Normalize null numbers to make serialization easier. -1 isn't valid values for LineNumber or Column.
        var lineNumber = info.GetInt64(nameof(LineNumber));
        LineNumber = lineNumber < 0 ? null : lineNumber;
        var column = info.GetInt64(nameof(Column));
        Column = column < 0 ? null : column;
    }

    public override string Message => ComposeMessage();

    public string? Reason => base.Message.Length == 0 ? null : base.Message; // we get the storage of a string for free

    public PersistenceErrorCode ErrorCode { get; }

    public string? MsappEntryFullPath { get; init; }

    public long? LineNumber { get; init; }

    /// <summary>
    /// The column on the line where the exception occurred.
    /// When this exception represents a JSON error, this is actually the `BytePositionInLine`. Depending on encoding, the byte position may be different than the column.
    /// </summary>
    public long? Column { get; init; }

    public string? JsonPath { get; init; }

    private string ComposeMessage()
    {
        var sb = new StringBuilder();
        sb.Append('[');
        sb.Append((int)ErrorCode);
        sb.Append(':');
        sb.Append(ErrorCode);
        sb.Append("] ");
        sb.Append(ErrorCode.GetDefaultExceptionMessage());

        if (Reason != null)
        {
            sb.Append(' ');
            sb.Append(Reason);
        }

        if (LineNumber != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, " Line: {0};", LineNumber.Value);
        }

        if (Column != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, " Column: {0};", Column.Value);
        }

        if (MsappEntryFullPath != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, " MsappEntry: {0};", MsappEntryFullPath);
        }

        if (JsonPath != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, " JsonPath: {0};", JsonPath);
        }

        return sb.ToString();
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(ErrorCode), ErrorCode);
        info.AddValue(nameof(MsappEntryFullPath), MsappEntryFullPath);
        info.AddValue(nameof(JsonPath), JsonPath);
        info.AddValue(nameof(LineNumber), LineNumber ?? -1);
        info.AddValue(nameof(Column), Column ?? -1);

        base.GetObjectData(info, context);
    }

    internal static PaPersistenceException FromYamlException(YamlException ex, PersistenceErrorCode errorCode)
    {
        return ex.Start.Equals(Mark.Empty)
            ? new PaPersistenceException(errorCode, ex.Message, ex)
            : new PaPersistenceException(errorCode, ex.Message, ex)
            {
                LineNumber = ex.Start.Line,
                Column = ex.Start.Column,
            };
    }
}
