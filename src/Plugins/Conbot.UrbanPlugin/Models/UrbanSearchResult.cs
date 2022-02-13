using System.Collections.Generic;

using Newtonsoft.Json;

namespace Conbot.UrbanPlugin;

public class UrbanSearchResult
{
    [JsonProperty("tags")]
    public IEnumerable<string> Tags { get; set; } = null!;

    [JsonProperty("result_type")]
    public string ResultType { get; set; } = string.Empty;

    [JsonProperty("list")]
    public IEnumerable<UrbanResult> Results { get; set; } = null!;

    [JsonProperty("sounds")]
    public IEnumerable<string> Sounds { get; set; } = null!;
}