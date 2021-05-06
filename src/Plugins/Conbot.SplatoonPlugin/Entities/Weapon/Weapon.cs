using Newtonsoft.Json;

namespace Conbot.SplatoonPlugin
{
    public record Weapon(
        string Key,
        int Id,
        string Name,
        WeaponType Type,
        SubWeapon SubWeapon,
        WeaponSpecial Special,
        string? ReskinOfKey,
        string MainWeaponKey,
        WeaponPowerUp MainPowerUp)
    {
        [JsonIgnore]
        public Weapon? ReskinOf { get; internal set; }

        [JsonIgnore]
        public Weapon? MainWeapon { get; internal set; }
    }
}
