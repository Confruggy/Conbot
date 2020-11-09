using Conbot.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conbot.PrefixPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services.AddHostedService<PrefixPluginService>();
            services.AddDbContext<PrefixContext>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder) { }
    }
}