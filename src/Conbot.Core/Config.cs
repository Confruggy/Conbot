using System.IO;
using Discord;
using Nett;

namespace Conbot
{
    public class Config
    {
        public string Token { get; set; }
        public string Secret { get; set; }
        public int TotalShards { get; set; } = 1;
        public LogSeverity LogSeverity { get; set; } = LogSeverity.Verbose;

        public static Config GetOrCreate(string filepath)
        {
            if (!File.Exists(filepath))
            {
                var config = new Config();
                Toml.WriteFile(config, filepath);
                return config;
            }
            else return Toml.ReadFile<Config>(filepath);
        }

        public void Save(string filepath) => Toml.WriteFile(this, filepath);
    }
}
