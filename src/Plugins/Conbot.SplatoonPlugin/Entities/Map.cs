using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin
{
    public record Map(
        [property: JsonProperty("id")] int Id,
        [property: JsonProperty("name")] string Name)
    {
        [JsonProperty("image")]
        private readonly string _imageUrl = string.Empty;

        public string ImageUrl => $"https://splatoon2.ink/{_imageUrl}";
    }
}
