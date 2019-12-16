using System.Threading.Tasks;
using Discord;
using Discord.Net;

namespace Conbot.Extensions
{
    public static class MessageExtensions
    {
        public static async Task<bool> TryAddReactionAsync(this IUserMessage message, IEmote emote,
            RequestOptions options = null)
        {
            try
            {
                await message.AddReactionAsync(emote, options).ConfigureAwait(false);
            }
            catch (HttpException)
            {
                return false;
            }
            return true;
        }


        public static async Task<bool> TryRemoveReactionAsync(this IUserMessage message, IEmote emote, IUser user,
            RequestOptions options = null)
        {
            try
            {
                await message.RemoveReactionAsync(emote, user, options).ConfigureAwait(false);
            }
            catch (HttpException)
            {
                return false;
            }
            return true;
        }

        public static async Task<bool> TryRemoveAllReactionsAsync(this IUserMessage message, RequestOptions options = null)
        {
            try
            {
                await message.RemoveAllReactionsAsync(options).ConfigureAwait(false);
            }
            catch (HttpException)
            {
                return false;
            }
            return true;
        }
    }
}
