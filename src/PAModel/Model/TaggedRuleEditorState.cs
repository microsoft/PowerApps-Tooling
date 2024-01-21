// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.Model;

public record TaggedRuleEditorState : RuleEditorState
{
    public string Tag { get; init; }
}
