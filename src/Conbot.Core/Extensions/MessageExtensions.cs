using System.Threading.Tasks;

using Disqord;
using Disqord.Rest;

namespace Conbot.Extensions;

public static class MessageExtensions
{
    public static async Task<bool> TryDeleteAsync(this IMessage message, IRestRequestOptions? options = null)
    {
        try
        {
            await message.DeleteAsync(options);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static async Task<bool> TryAddReactionAsync(this IMessage message, LocalEmoji emoji,
        IRestRequestOptions? options = null)
    {
        try
        {
            await message.AddReactionAsync(emoji, options);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static async Task<bool> TryRemoveReactionAsync(this IMessage message, LocalEmoji emoji,
        Snowflake userId, IRestRequestOptions? options = null)
    {
        try
        {
            await message.RemoveReactionAsync(emoji, userId, options);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static async Task<bool> TryClearAllReactionsAsync(this IMessage message, LocalEmoji? emoji = null,
        IRestRequestOptions? options = null)
    {
        try
        {
            await message.ClearReactionsAsync(emoji, options);
        }
        catch
        {
            return false;
        }

        return true;
    }
}