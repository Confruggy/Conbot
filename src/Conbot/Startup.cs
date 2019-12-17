using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Conbot.Services.Commands;
using Conbot.Services.Discord;
using Conbot.Services.Urban;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;
using Conbot.Services.Help;
using Conbot.Services.Interactive;
using Conbot.Services;
using Conbot.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Conbot
{
    public class Startup
    {
        private readonly Config _config;

        public Startup(Config config)
        {
            _config = config;
        }

        public async Task RunAsync()
        {
            var host = new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .ConfigureServices(services => ConfigureServices(services))
                .UseConsoleLifetime()
                .Build();

            UpdateDatabase(host.Services);

            await host.RunAsync();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services
                //Config
                .AddSingleton(_config)
                .AddSingleton(new DiscordSocketConfig
                {
                    TotalShards = _config.TotalShards,
                    LogLevel = LogSeverity.Debug,
                    MessageCacheSize = 100,
                    DefaultRetryMode = RetryMode.AlwaysRetry
                })
                .AddSingleton(new CommandServiceConfig
                {
                    CaseSensitiveCommands = false,
                    DefaultRunMode = RunMode.Sync,
                    LogLevel = LogSeverity.Debug
                })

                //Discord
                .AddSingleton<DiscordShardedClient>()

                //Services
                .AddHostedService<DiscordService>()
                .AddSingleton<CommandService>()
                .AddHostedService<CommandHandlingService>()
                .AddSingleton<InteractiveService>()
                .AddHostedService<BackgroundServiceStarter<InteractiveService>>()
                .AddSingleton<UrbanService>()
                .AddSingleton<HelpService>()

                //DbContext
                .AddDbContext<ConbotContext>()

                //Utils
                .AddSingleton<Random>();
        }

        private static void UpdateDatabase(IServiceProvider services)
        {
            using var serviceScope = services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<ConbotContext>();
            context.Database.Migrate();
        }
    }
}