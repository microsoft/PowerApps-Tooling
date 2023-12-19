using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml2;

public class YamlPocoConverter
{
    private readonly Lazy<ISerializer> _serializer =
        new (() => new SerializerBuilder()
            .WithQuotingNecessaryStrings(true)
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithEventEmitter(next => new MultilineStyleEmitter(next))
            .Build());

    private readonly Lazy<IDeserializer> _deserializer =
        new (new DeserializerBuilder()
            .WithDuplicateKeyChecking()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build());

    public ISerializer Serializer => _serializer.Value;
    public IDeserializer Deserializer => _deserializer.Value;

    public string Canonicalize<T>(string yaml)
    {
        return Serializer.Serialize(Deserializer.Deserialize<T>(yaml));
    }
}
