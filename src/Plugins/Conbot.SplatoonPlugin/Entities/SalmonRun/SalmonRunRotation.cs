using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin
{
    public class SalmonRunRotation
    {
        [JsonProperty("start_time")]
        private readonly long _startTime;

        public DateTimeOffset StartTime => DateTimeOffset.FromUnixTimeSeconds(_startTime);

        [JsonProperty("end_time")]
        private readonly long _endTime;

        public DateTimeOffset EndTime => DateTimeOffset.FromUnixTimeSeconds(_endTime);

        [JsonProperty("stage")]
        public Map? Map { get; init; }

        [JsonProperty("weapons")]
        private readonly IEnumerable<WeaponObject>? _weapons;

        private class WeaponObject
        {
            [JsonProperty("weapon")]
            internal readonly WeaponSummary Weapon = null!;
        }

        public IEnumerable<WeaponSummary> Weapons
            => _weapons is not null ? _weapons.Select(x => x.Weapon) : Enumerable.Empty<WeaponSummary>();
    }
}
