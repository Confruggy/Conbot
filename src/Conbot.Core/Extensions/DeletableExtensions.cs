using System.Threading.Tasks;

using Discord;
using Discord.Net;

namespace Conbot.Extensions
{
    public static class DeletableExtensions
    {
        public static async Task<bool> TryDeleteAsync(this IDeletable deletable)
        {
            try
            {
                await deletable.DeleteAsync();
                return true;
            }
            catch (HttpException)
            {
                return false;
            }
        }
    }
}
