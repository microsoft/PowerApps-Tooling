// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record Header
{
    [SetsRequiredMembers]
    public Header()
    {
    }

    public required Version DocVersion { get; init; } = new Version("1.337");
    public required Version MinVersionToLoad { get; init; } = new Version("1.337");
    public required Version MSAppStructureVersion { get; init; } = new Version("2.2.1");

    public AnalysisOptionsHeader AnalysisOptions { get; init; } = new AnalysisOptionsHeader();

    public record AnalysisOptionsHeader
    {
        [SetsRequiredMembers]
        public AnalysisOptionsHeader()
        {
        }

        public required bool DataflowAnalysisEnabled { get; init; } = true;
        public required bool DataflowAnalysisFlagStateToggledByUser { get; init; }
    }
}
