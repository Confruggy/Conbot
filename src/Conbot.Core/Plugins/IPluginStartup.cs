using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conbot.Plugins;

public interface IPluginStartup
{
    void ConfigureServices(HostBuilderContext hostingContext, IServiceCollection services);
    void BuildConfiguration(IConfigurationBuilder builder);
}