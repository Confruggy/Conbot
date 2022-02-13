using System;

using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin;

public class Merchandise
{
    [JsonProperty("id")]
    public ulong Id { get; set; }

    [JsonProperty("end_time")]
    private readonly long _endTime;

    public DateTimeOffset EndTime => DateTimeOffset.FromUnixTimeSeconds(_endTime);

    [JsonProperty("gear")]
    public Gear Gear { get; init; } = null!;

    [JsonProperty("original_gear")]
    public OriginalGear OriginalGear { get; init; } = null!;

    [JsonProperty("skill")]
    public Skill Skill { get; init; } = null!;

    [JsonProperty("kind")]
    public GearKind Kind { get; init; }

    [JsonProperty("price")]
    public int Price { get; init; }
}