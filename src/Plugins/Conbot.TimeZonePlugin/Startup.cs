using Conbot.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Conbot.TimeZonePlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddHostedService<TimeZonePluginService>()
                .AddDbContext<TimeZoneContext>()
                .AddSingleton(DateTimeZoneProviders.Tzdb);
        }

        public void BuildConfiguration(IConfigurationBuilder builder)
        {
            
        }
    }
}