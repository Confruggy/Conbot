using Conbot.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conbot.TagPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services.AddHostedService<TagPluginService>();
                        
            services.AddDbContext<TagContext>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder)
        {
            
        }
    }
}