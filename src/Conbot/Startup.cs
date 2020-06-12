using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Conbot.Services.Commands;
using Conbot.Services.Discord;
using Conbot.Services.Urban;
using Discord;
using Discord.WebSocket;
using Serilog;
using Conbot.Services.Help;
using Conbot.Services.Interactive;
using Conbot.Services;
using Conbot.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Qmmands;

namespace Conbot
{
    public class Startup
    {
        public async Task StartAsync()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(BuildConfiguration)
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration))
                .ConfigureServices(ConfigureServices)
                .UseConsoleLifetime()
                .Build();

            UpdateDatabase(host.Services);

            await host.RunAsync();
        }
        
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                //Config
                .AddSingleton(new DiscordSocketConfig
                {
                    TotalShards = hostingContext.Configuration.GetValue<int>("Discord:TotalShards"),
                    LogLevel = LogSeverity.Debug,
                    MessageCacheSize = hostingContext.Configuration.GetValue<int>("Discord:MessageCacheSize"),
                    DefaultRetryMode = RetryMode.AlwaysRetry
                })
                .AddSingleton(new CommandServiceConfiguration
                {
                    StringComparison = StringComparison.OrdinalIgnoreCase
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

        public void BuildConfiguration(IConfigurationBuilder builder)
        {
            builder
                .AddJsonFile("appsettings.json");
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