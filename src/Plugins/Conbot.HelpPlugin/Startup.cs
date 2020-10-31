using Conbot.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conbot.HelpPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddHostedService<HelpPluginService>()
                .AddSingleton<HelpService>();

        }

        public void BuildConfiguration(IConfigurationBuilder builder)
        {
            
        }
    }
}