using YamlDotNet.Serialization;

namespace PAModelTests.Yaml2SerializerTests.YamlPocoTypes;

public class OrderedObject
{
    [YamlMember(Order = 3)]
    public int Arg3 { get; set; }

    [YamlMember(Order = 2)]
    public string Arg2 { get; set; }

    [YamlMember(Order = 1)]
    public bool Arg1 { get; set; }
}
