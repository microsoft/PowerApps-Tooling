// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.PowerPlatform.Formulas.Tools.Model;

[DebuggerDisplay("{Category} / {Property} / {InvariantScript}")]
public record RuleEditorState
{
    public string Category { get; init; }
    public string Property { get; init; }
    public string NameMap { get; init; }
    public string InvariantScript { get; init; }
    public string RuleProviderType { get; init; }
    public IList<TaggedRuleEditorState> TaggedRuleArray { get; init; }
}
