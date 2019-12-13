using System;
using System.IO;
using System.Threading.Tasks;
using Nett;
using Serilog;

namespace Conbot
{
    class Program
    {
        static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "log.log"))
                .MinimumLevel.Information()
                .CreateLogger();

            var config = ReadConfig();

            await new Startup(config).RunAsync();

            Log.CloseAndFlush();
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