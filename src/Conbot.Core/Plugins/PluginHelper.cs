using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Conbot.Plugins
{
    public static class PluginHelper
    {
        public static IEnumerable<Assembly> LoadPluginAssemblies(string path)
        {
            var assemblies = new List<Assembly>();

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                var name = Path.GetFileName(dir);
                var assembly = LoadPluginAssembly(Path.Combine(dir, $"{name}.dll"));
                assemblies.Add(assembly);
            }

            return assemblies;
        }

        public static Assembly LoadPluginAssembly(string pluginLocation)
        {
            var loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        public static void InstallPlugins(Assembly assembly, IHostBuilder host)
        {
            var types = assembly.GetTypes().Where(x => typeof(IPluginStartup).IsAssignableFrom(x));
            foreach (var type in types)
            {
                var pluginStartup = Activator.CreateInstance(type) as IPluginStartup;
                host.ConfigureServices(pluginStartup.ConfigureServices);
                host.ConfigureAppConfiguration(pluginStartup.BuildConfiguration);
            }
        }

        public static void InstallPlugins(IEnumerable<Assembly> assemblies, IHostBuilder host)
        {
            foreach (var assembly in assemblies)
                InstallPlugins(assembly, host);
        }
    }
}