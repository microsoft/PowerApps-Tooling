// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Model class for header.json file in msapp archive.
/// See same class in DocumentServer.Core for updated schema.
/// </summary>
internal sealed record HeaderJson
{
    public required Version DocVersion { get; init; }
    public required Version MinVersionToLoad { get; init; }
    public required Version MSAppStructureVersion { get; init; }
    public DateTime? LastSavedDateTimeUTC { get; init; }

    public AnalysisOptionsHeader? AnalysisOptions { get; init; }

    public sealed record AnalysisOptionsHeader
    {
        public bool DataflowAnalysisEnabled { get; init; }
        public bool DataflowAnalysisFlagStateToggledByUser { get; init; }
    }
}
