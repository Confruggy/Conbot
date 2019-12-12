using System;
using System.IO;
using System.Threading.Tasks;
using Nett;

namespace Conbot
{
    class Program
    {
        static async Task Main()
        {
            var config = ReadConfig();

            await new Startup(config).RunAsync();
            await Task.Delay(-1);
        }

        private static Config ReadConfig()
        {
            string configFilePath = Path.Combine(AppContext.BaseDirectory, "config.toml");

            var config = Config.GetOrCreate(configFilePath);

            if (string.IsNullOrWhiteSpace(config.Token))
            {
                Console.Write("Enter your bot token: ");
                string token = Console.ReadLine();
                config.Token = token;
                Toml.WriteFile(config, configFilePath);
            }

            return config;
        }
    }
}