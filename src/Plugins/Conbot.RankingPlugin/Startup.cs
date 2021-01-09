using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

namespace Conbot.RankingPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddDbContext<RankingContext>()
                .AddHostedService<RankingPluginService>()
                .AddSingleton<RankingService>()
                .AddHostedService<BackgroundServiceStarter<RankingService>>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder)
        {
            builder
                .AddJsonFile(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "pluginsettings.json"));
        }
    }
}