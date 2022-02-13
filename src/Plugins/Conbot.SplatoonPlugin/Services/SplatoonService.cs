using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin;

public sealed class SplatoonService : IDisposable
{
    private readonly ILogger<SplatoonService> _logger;
    private readonly string _baseUrl;
    private readonly HttpClient _client;
    private Schedules? _schedules;
    private SalmonRunSchedules? _salmonRunSchedules;
    private IEnumerable<Merchandise>? _merchandises;

    public IEnumerable<Weapon> Weapons { get; }

    public SplatoonService(IConfiguration configuration, ILogger<SplatoonService> logger)
    {
        _logger = logger;

        _baseUrl = configuration["SplatoonPlugin:Splatoon2InkBaseUrl"];
        _client = new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Conbot (+https://github.com/Confruggy/Conbot)");

        Weapons = InitializeWeapons();
    }

    public async Task<Schedules?> GetSchedulesAsync()
    {
        if (_schedules is not null && _schedules.RegularRotations.FirstOrDefault()?.EndTime >= DateTimeOffset.UtcNow)
            return _schedules;

        try
        {
            string json = await _client.GetStringAsync($"{_baseUrl}/data/schedules.json");
            _schedules = JsonConvert.DeserializeObject<Schedules>(json);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed getting getting Splatoon 2 schedules");
        }

        return _schedules;
    }

    public async Task<SalmonRunSchedules?> GetSalmonRunSchedulesAsync()
    {
        if (_salmonRunSchedules is not null &&
            _salmonRunSchedules.Rotations.FirstOrDefault()?.EndTime >= DateTimeOffset.UtcNow)
        {
            return _salmonRunSchedules;
        }

        try
        {
            string json = await _client.GetStringAsync($"{_baseUrl}/data/coop-schedules.json");
            _salmonRunSchedules = JsonConvert.DeserializeObject<SalmonRunSchedules>(json);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed getting getting Splatoon 2 Salmon Run schedules");
        }

        return _salmonRunSchedules;
    }

    public async Task<IEnumerable<Merchandise>?> GetMerchandisesAsync()
    {
        if (_merchandises is not null &&
            _merchandises.FirstOrDefault()?.EndTime >= DateTimeOffset.UtcNow)
        {
            return _merchandises;
        }

        try
        {
            string json = await _client.GetStringAsync($"{_baseUrl}/data/merchandises.json");
            _merchandises =
                JsonConvert.DeserializeObject<Dictionary<string, IEnumerable<Merchandise>>>(json)?["merchandises"];
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed getting getting Splatoon 2 merchandises");
        }

        return _merchandises;
    }

    public Weapon? GetWeapon(int id)
        => Weapons.FirstOrDefault(x => x.Id == id);

    public Weapon? GetWeapon(string key)
        => Weapons.FirstOrDefault(x => x.Key == key);

    private static IEnumerable<Weapon> InitializeWeapons()
    {
        string json = File.ReadAllText(
            Path.Combine(Path.GetDirectoryName(typeof(SplatoonService).Assembly.Location)!, "weapons.json"));
        var weapons = JsonConvert.DeserializeObject<IEnumerable<Weapon>>(json)?.ToArray();

        if (weapons is null)
        {
            return Enumerable.Empty<Weapon>();
        }

        foreach (var weapon in weapons)
        {
            weapon.MainWeapon = weapons.First(x => x.Key == weapon.MainWeaponKey);

            if (weapon.ReskinOfKey is not null)
                weapon.ReskinOf = weapons.First(x => x.Key == weapon.ReskinOfKey);
        }

        return weapons;
    }

    public void Dispose() => _client.Dispose();
}