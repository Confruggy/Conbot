using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Conbot.UrbanPlugin
{
    public class UrbanSearchResult
    {
        [JsonProperty("tags")]
        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();

        [JsonProperty("result_type")]
        public string ResultType { get; set; } = string.Empty;

        [JsonProperty("list")]
        public IEnumerable<UrbanResult> Results { get; set; } = Enumerable.Empty<UrbanResult>();

        [JsonProperty("sounds")]
        public IEnumerable<string> Sounds { get; set; } = Enumerable.Empty<string>();
    }
}