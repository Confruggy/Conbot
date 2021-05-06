using Humanizer;

using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin
{
    public record Skill(
        [property: JsonProperty("id")] int Id,
        [property: JsonProperty("Name")] string Name)
    {
        public string EmoteName => string.Concat(Name.Split(new char[] { '(', ')' })).Pascalize();
    }
}
