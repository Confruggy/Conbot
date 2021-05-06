using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin
{
    public record Rule(
        [property: JsonProperty("name")] string Name,
        [property: JsonProperty("key")] string Key
    );
}
