using Conbot.Plugins;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conbot.ModerationPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddDbContext<ModerationContext>()
                .AddHostedService<ModerationPluginService>()
                .AddHostedService<MuteService>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder) { }
    }
}
