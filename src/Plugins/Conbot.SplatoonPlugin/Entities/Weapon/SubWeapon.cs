namespace Conbot.SplatoonPlugin;

public record SubWeapon(
    string Name,
    string Key)
{
    public string EmoteName => string.Concat(Name.Split(new char[] { ' ', '-' }));
}