// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

public class PaDiagnosticsException : Exception
{
    public PaDiagnosticsException(PaDiagnostic error)
        : this(null!, error)
    {
    }

    public PaDiagnosticsException(IEnumerable<PaDiagnostic> errors)
        : this(null!, errors)
    {
    }

    public PaDiagnosticsException(Exception innerException, PaDiagnostic error)
        : this(innerException, new[] { error ?? throw new ArgumentNullException(nameof(error)) })
    {
    }

    public PaDiagnosticsException(Exception innerException, IEnumerable<PaDiagnostic> errors)
        : base(null, innerException)
    {
        _ = errors ?? throw new ArgumentNullException(nameof(errors));
        Errors = errors.Where(e => e is not null).ToArray();
        if (Errors.Count == 0)
        {
            throw new ArgumentException($"{nameof(errors)} contains no non-null entries.", nameof(errors));
        }
    }

    public override string Message => string.Join(Environment.NewLine, Errors.Select(e => e.ToLoggerSafeString()));

    public IReadOnlyList<PaDiagnostic> Errors { get; }

    internal static PaDiagnosticsException FromYamlException(YamlException ex, PersistenceErrorCode errorCode, string? originToolOrFilePath)
    {
        var origin = new PaDiagnosticOrigin(originToolOrFilePath)
        {
            Start = ex.Start.Equals(Mark.Empty) ? null : (ex.Start.Line, ex.Start.Column),
            End = ex.End.Equals(Mark.Empty) ? null : (ex.End.Line, ex.End.Column),
        };

        return new(ex, new PaDiagnostic(errorCode, ex.Message)
        {
            Origin = origin.ToolOrFilePath != null || origin.Start.HasValue ? origin : null,
        });
    }
}
