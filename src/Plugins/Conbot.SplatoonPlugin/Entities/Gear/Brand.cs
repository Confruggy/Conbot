using Humanizer;

using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin
{
    public record Brand(
        [property: JsonProperty("id")] int Id,
        [property: JsonProperty("name")] string Name,
        [property: JsonProperty("frequent_skill")] Skill FrequentSkill)
    {
        public string EmoteName => Name.Replace("-", "").Pascalize();
    }
}
