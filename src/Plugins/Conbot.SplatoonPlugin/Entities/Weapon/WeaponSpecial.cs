namespace Conbot.SplatoonPlugin
{
    public record WeaponSpecial(
        string Name,
        string Key)
    {
        public string EmoteName => string.Concat(Name.Split(new char[] { ' ', '-' }));
    }
}
