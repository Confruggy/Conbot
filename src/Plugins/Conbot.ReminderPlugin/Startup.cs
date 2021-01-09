using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

namespace Conbot.ReminderPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddHostedService<ReminderPluginService>()
                .AddDbContext<ReminderContext>()
                .AddHostedService<ReminderService>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder) { }
    }
}