using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

namespace Conbot.UrbanPlugin;

public class Startup : IPluginStartup
{
    public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
    {
        services
            .AddSingleton<UrbanService>();
    }

    public void BuildConfiguration(IConfigurationBuilder builder)
    {
        builder
            .AddJsonFile(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "pluginsettings.json"));
    }
}