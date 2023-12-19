using YamlDotNet.Serialization;

namespace PAModelTests.Yaml2SerializerTests.YamlPocoTypes;

public class IgnoreSomePropertiesObject
{
    public string IncludeMe { get; set; }

    [YamlIgnore]
    public string IgnoreMe { get; set; }
}
