using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

namespace Conbot.HelpPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddHostedService<HelpPluginService>()
                .AddSingleton<HelpService>()
                .AddHostedService<BackgroundServiceStarter<HelpService>>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder) { }
    }
}
