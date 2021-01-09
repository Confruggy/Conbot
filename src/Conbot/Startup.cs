using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Commands;
using Conbot.Interactive;
using Conbot.Plugins;

using Discord;
using Discord.WebSocket;

using Qmmands;

using Serilog;

namespace Conbot
{
    public class Startup
    {
        public async Task StartAsync()
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(BuildConfiguration)
                .UseSerilog((hostingContext, loggerConfiguration)
                    => loggerConfiguration
                        .ReadFrom
                        .Configuration(hostingContext.Configuration))
                .ConfigureServices(ConfigureServices)
                .UseConsoleLifetime();

            var assemblies = PluginHelper.LoadPluginAssemblies("Plugins");
            PluginHelper.InstallPlugins(assemblies, builder);

            var host = builder.Build();

            await host.RunAsync();
        }

        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            //Config
            services
                .AddSingleton(new DiscordSocketConfig
                {
                    TotalShards = hostingContext.Configuration.GetValue<int>("Discord:TotalShards"),
                    LogLevel = LogSeverity.Debug,
                    MessageCacheSize = hostingContext.Configuration.GetValue<int>("Discord:MessageCacheSize"),
                    DefaultRetryMode = RetryMode.AlwaysRetry
                })
                .AddSingleton(new CommandServiceConfiguration { StringComparison = StringComparison.OrdinalIgnoreCase });

            //Discord
            services
                .AddSingleton<DiscordShardedClient>();

            //Services
            services
                .AddHostedService<DiscordService>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<SlashCommandService>()
                .AddHostedService<BackgroundServiceStarter<CommandHandlingService>>()
                .AddSingleton<InteractiveService>()
                .AddHostedService<BackgroundServiceStarter<InteractiveService>>();

            //Utils
            services
                .AddSingleton<Random>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder)
        {
            builder
                .AddJsonFile("appsettings.json");
        }
    }
}
