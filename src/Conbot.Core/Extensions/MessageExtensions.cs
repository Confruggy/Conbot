using System.Threading.Tasks;
using Discord;
using Discord.Net;

namespace Conbot.Core.Extensions
{
    public static class MessageExtensions
    {
        public static async Task<bool> TryAddReactionAsync(this IUserMessage message, IEmote emote, RequestOptions options = null)
        {
            try
            {
                await message.AddReactionAsync(emote, options).ConfigureAwait(false);
                return true;
            }
            catch (HttpException)
            {
                return false;
            }
        }
    }
}
