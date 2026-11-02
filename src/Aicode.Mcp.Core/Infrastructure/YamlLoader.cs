using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Infrastructure;

public static class YamlLoader
{
    private static readonly IDeserializer Deserializer =
        new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

    /// <summary>Loads a YAML file, or returns null when the file does not exist or is empty.</summary>
    public static T? Load<T>(string path) where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return Deserializer.Deserialize<T>(File.ReadAllText(path));
    }
}
