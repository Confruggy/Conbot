using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

using NodaTime;

namespace Conbot.TimeZonePlugin;

public class Startup : IPluginStartup
{
    public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
    {
        services
            .AddDbContext<TimeZoneContext>()
            .AddSingleton(DateTimeZoneProviders.Tzdb);
    }

    public void BuildConfiguration(IConfigurationBuilder builder)
    {
    }
}
