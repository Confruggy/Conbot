using System.Linq;

using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin;

public record Gear(
    [property: JsonProperty("id")] int Id,
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("rarity")] int Rarity,
    [property: JsonProperty("kind")] GearKind Kind,
    [property: JsonProperty("brand")] Brand Brand)
{
    [JsonProperty("image")]
    private readonly string _image = string.Empty;

    public string ImageUrl => $"https://splatoon2.ink/assets/img/splatnet/{_image.Split('/').Last()}";
}
