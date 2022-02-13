using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Hosting;

namespace Conbot.Plugins;

public static class PluginHelper
{
    public static IEnumerable<Assembly> LoadPluginAssemblies(string path)
    {
        var assemblies = new List<Assembly>();

        foreach (string dir in Directory.EnumerateDirectories(path))
        {
            string name = Path.GetFileName(dir);
            var assembly = Assembly.LoadFrom(Path.Combine(Path.GetFullPath(dir), $"{name}.dll"));
            assemblies.Add(assembly);
        }

        return assemblies;
    }

    public static void InstallPlugins(Assembly assembly, IHostBuilder host)
    {
        var types = assembly.GetTypes().Where(x => typeof(IPluginStartup).IsAssignableFrom(x));
        foreach (var type in types)
        {
            if (Activator.CreateInstance(type) is not IPluginStartup pluginStartup)
                continue;

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