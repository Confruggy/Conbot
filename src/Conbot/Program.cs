using System;
using System.IO;
using System.Threading.Tasks;
using Conbot.Core;
using Microsoft.Extensions.DependencyInjection;
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

            var services = new ServiceCollection();
            ConfigureServices(services);

            await new ConbotClient(_config).StartAsync(services);
            await Task.Delay(-1);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Random>();
        }

        private bool ReadConfig()
        {
            string configFilePath = Path.Combine(AppContext.BaseDirectory, "config.toml");

            if (!File.Exists(configFilePath))
            {
                _config = new Config();
                Toml.WriteFile(_config, configFilePath);
            }
            else
            {
                try
                {
                    _config = Toml.ReadFile<Config>(configFilePath);
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
                Toml.WriteFile(_config, configFilePath);
            }

            return true;
        }
    }
}