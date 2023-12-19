// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml2;

/// <summary>
/// Used to instruct YamlDotNet to serialize multiline strings with
/// '|' (literal style) instead of '>' (folded style)
/// </summary>
public class MultilineStyleEmitter : ChainedEventEmitter
{
    public MultilineStyleEmitter(IEventEmitter nextEmitter)
        : base(nextEmitter)
    {
    }

    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {

        if (typeof(string).IsAssignableFrom(eventInfo.Source.Type))
        {
            var value = eventInfo.Source.Value as string;
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Any(Parser.CharacterUtils.IsLineTerm))
                    eventInfo = new ScalarEventInfo(eventInfo.Source)
                    {
                        Style = ScalarStyle.Literal,
                    };
            }
        }

        nextEmitter.Emit(eventInfo, emitter);
    }
}
