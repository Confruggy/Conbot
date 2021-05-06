using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin
{
    public record GameMode(
        [property: JsonProperty("name")] string Name,
        [property: JsonProperty("key")] string Key
    );
}
