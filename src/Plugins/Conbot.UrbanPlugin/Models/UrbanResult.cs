using Newtonsoft.Json;

namespace Conbot.UrbanPlugin
{
    public class UrbanResult
    {
        [JsonProperty("defid")]
        public int Id { get; set; }

        [JsonProperty("word")]
        public string Word { get; set; } = string.Empty;

        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;

        [JsonProperty("permalink")]
        public string Permalink { get; set; } = string.Empty;

        [JsonProperty("definition")]
        public string Definition { get; set; } = string.Empty;

        [JsonProperty("example")]
        public string Example { get; set; } = string.Empty;

        [JsonProperty("thumbs_up")]
        public int ThumbsUp { get; set; }

        [JsonProperty("thumbs_down")]
        public int ThumbsDown { get; set; }
    }
}