using YamlDotNet.Serialization;

namespace PAModelTests.Yaml2SerializerTests.YamlPocoTypes;

public class DuplicateKeyObject
{
    [YamlMember(Alias = "collision", Description = "asdf")]
    public string Arg1 { get; set; }

    [YamlMember(Alias = "collision")]
    public string Arg2 { get; set; }
}
