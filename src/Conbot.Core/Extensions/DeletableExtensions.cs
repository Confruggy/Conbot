using System.Threading.Tasks;
using Discord;
using Discord.Net;

namespace Conbot.Core.Extensions
{
    public static class DeletableExtensions
    {
        public static async Task<bool> TryDeleteAsync(this IDeletable deletable)
        {
            try
            {
                await deletable.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            catch (HttpException)
            {
                return false;
            }
        }
    }
}
