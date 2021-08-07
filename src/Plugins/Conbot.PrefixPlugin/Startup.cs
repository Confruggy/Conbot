using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Plugins;

using Disqord.Bot;

namespace Conbot.PrefixPlugin
{
    public class Startup : IPluginStartup
    {
        public void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services
                .AddSingleton<IPrefixProvider>(x => x.GetRequiredService<CustomPrefixProvider>())
                .AddDbContext<PrefixContext>();
        }

        public void BuildConfiguration(IConfigurationBuilder builder) { }
    }
}
