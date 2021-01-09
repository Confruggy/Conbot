using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

namespace Conbot.UrbanPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddHostedService<UrbanPluginService>()
                .AddSingleton<UrbanService>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder) { }
    }
}