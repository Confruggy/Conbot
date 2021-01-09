using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

namespace Conbot.RngPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services.AddHostedService<RngPluginService>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder) { }
    }
}