using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Interactive;
using Conbot.Plugins;

using Disqord.Bot.Hosting;

using Serilog;

namespace Conbot;

public class Startup
{
    public static async Task StartAsync()
    {
        var assemblies = PluginHelper.LoadPluginAssemblies("Plugins");

        var builder = new HostBuilder()
            .ConfigureAppConfiguration(BuildConfiguration)
            .UseSerilog((hostingContext, loggerConfiguration)
                => loggerConfiguration
                    .ReadFrom
                    .Configuration(hostingContext.Configuration))
            .ConfigureDiscordBot<ConbotBot>((context, bot) =>
            {
                bot.Token = context.Configuration["Discord:Token"];
                bot.Prefixes = new[] { "!" };
                bot.ServiceAssemblies = assemblies.Append(typeof(InteractiveService).Assembly).ToList();
            })
            .ConfigureServices(ConfigureServices)
            .UseConsoleLifetime();

        PluginHelper.InstallPlugins(assemblies, builder);

        var host = builder.Build();

        await host.RunAsync();
    }

    public static void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
    {
        services
            .AddSingleton<Random>();
    }

    public static void BuildConfiguration(IConfigurationBuilder builder)
    {
        builder
            .AddJsonFile("appsettings.json");
    }
}