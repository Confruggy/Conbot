using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

namespace Conbot.RpsPlugin;

public class Startup : IPluginStartup
{
    public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
    {
    }

    public void BuildConfiguration(IConfigurationBuilder builder)
    {
    }
}