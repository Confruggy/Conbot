using System;
using System.Threading.Tasks;

namespace Conbot
{
    internal static class Program
    {
        private static async Task Main()
        {
            Environment.CurrentDirectory = AppContext.BaseDirectory;
            await new Startup().StartAsync();
        }
    }
}