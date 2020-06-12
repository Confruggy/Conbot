using System;
using System.Threading.Tasks;

namespace Conbot
{
    class Program
    {
        static async Task Main()
        {
            Environment.CurrentDirectory = AppContext.BaseDirectory;
            await new Startup().StartAsync();
        }
    }
}