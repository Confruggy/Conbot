using System;
using System.IO;
using System.Threading.Tasks;
using Nett;

namespace Conbot
{
    class Program
    {
        private Config _config;

        static Task Main() => new Program().StartAsync();

        private async Task StartAsync()
        {
            if (!ReadConfig())
                return;

            await Task.Delay(0);
        }

        private bool ReadConfig()
        {
            if (!File.Exists("config.toml"))
            {
                Console.WriteLine("Config file is missing. Please provide a config file!");
                _config = new Config();
                Toml.WriteFile(_config, "config.toml");
            }
            else
            {
                try
                {
                    _config = Toml.ReadFile<Config>("config.toml");
                }
                catch
                {
                    Console.WriteLine("Config file is invalid. Please provide a valid config file!");
                    Console.ReadKey();
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(_config.Token))
            {
                Console.Write("Enter your bot token: ");
                string token = Console.ReadLine();
                _config.Token = token;
                Toml.WriteFile(_config, "config.toml");
            }
            
            return true;
        }
    }
}