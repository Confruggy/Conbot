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
                .AddSingleton<HelpPluginCommandFailedHandler>()
                .AddSingleton<ICommandFailedHandler>(x => x.GetRequiredService<HelpPluginCommandFailedHandler>());
        }

        public void BuildConfiguration(IConfigurationBuilder builder) { }
    }
}
