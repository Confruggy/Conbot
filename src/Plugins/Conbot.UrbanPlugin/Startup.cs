using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;
using System.IO;

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

        public void BuildConfiguration(IConfigurationBuilder builder)
        {
            builder
                .AddJsonFile(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "pluginsettings.json"));
        }
    }
}
