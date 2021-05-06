using System.IO;

using Conbot.Plugins;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conbot.SplatoonPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddHostedService<SplatoonPluginService>()
                .AddSingleton<SplatoonService>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder)
        {
            builder
                .AddJsonFile(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "pluginsettings.json"));
        }
    }
}
