using System.Collections.Generic;
using Newtonsoft.Json;

namespace Conbot.Services
{
    public class UrbanSearchResult
    {
        [JsonProperty("tags")]
        public IEnumerable<string> Tags { get; set; }

        [JsonProperty("result_type")]
        public string ResultType { get; set; }

        [JsonProperty("list")]
        public IEnumerable<UrbanResult> Results { get; set; }

        [JsonProperty("sounds")]
        public IEnumerable<string> Sounds { get; set; }
    }
}