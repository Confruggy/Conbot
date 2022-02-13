using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin;

public class SalmonRunSchedules
{
    [JsonProperty("details")]
    private readonly IEnumerable<SalmonRunRotation>? _detailedRotations;

    public IEnumerable<SalmonRunRotation> DetailedRotations
        => _detailedRotations ?? Enumerable.Empty<SalmonRunRotation>();

    [JsonProperty("schedules")]
    private readonly IEnumerable<SalmonRunRotation>? _rotations;

    public IEnumerable<SalmonRunRotation> Rotations
        => _rotations ?? Enumerable.Empty<SalmonRunRotation>();
}