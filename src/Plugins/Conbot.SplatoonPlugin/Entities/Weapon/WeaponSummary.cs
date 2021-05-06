using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin
{
    public record WeaponSummary(
        [property: JsonProperty("id")] int Id,
        [property: JsonProperty("name")] string Name
    );
}
