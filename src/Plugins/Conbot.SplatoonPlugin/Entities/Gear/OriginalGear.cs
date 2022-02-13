using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin;

public record OriginalGear(
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("price")] int? Price,
    [property: JsonProperty("rarity")] int Rarity,
    [property: JsonProperty("Skill")] Skill Skill
);