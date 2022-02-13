using Newtonsoft.Json;

using System;

namespace Conbot.SplatoonPlugin;

public class Rotation
{
    [JsonProperty("id")]
    public ulong Id { get; init; }

    [JsonProperty("game_mode")]
    public GameMode Mode { get; init; } = null!;

    [JsonProperty("rule")]
    public Rule Rule { get; init; } = null!;

    [JsonProperty("start_time")]
    private readonly long _startTime;

    public DateTimeOffset StartTime => DateTimeOffset.FromUnixTimeSeconds(_startTime);

    [JsonProperty("end_time")]
    private readonly long _endTime;

    public DateTimeOffset EndTime => DateTimeOffset.FromUnixTimeSeconds(_endTime);

    [JsonProperty("stage_a")]
    public Map MapA { get; init; } = null!;

    [JsonProperty("stage_b")]
    public Map MapB { get; init; } = null!;
}