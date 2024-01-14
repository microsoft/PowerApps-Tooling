using System.ComponentModel;
using YamlDotNet.Serialization;

namespace PAModelTests.Yaml2SerializerTests.YamlPocoTypes;

public class DefaultValuesObject
{
    public DefaultValuesObject()
    {
        // Note that default value handling is used for serialization, but not deserialization.
        // We need to initialize that value ourselves in the constructor or property init syntax
        Bar = "bar";
    }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public string Foo { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    [DefaultValue("bar")]
    public string Bar { get; set; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
    public string Baz { get; set; }
}
