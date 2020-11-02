using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Qmmands;

namespace Conbot.Commands
{
    public class ChannelTypeParser<T> : TypeParser<T> where T : class, IChannel
    {
        public override ValueTask<TypeParserResult<T>> ParseAsync(Parameter parameter, string value, CommandContext context)
        {
            var discordCommandContext = context as DiscordCommandContext;

            if (discordCommandContext.Guild == null)
                return TypeParserResult<T>.Unsuccessful("This command must be used in a server.");

            IChannel channel = null;

            if (MentionUtils.TryParseRole(value, out var id))
                channel = discordCommandContext.Guild.GetChannel(id);
            else if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                channel = discordCommandContext.Guild.GetChannel(id);
            else 
            {
                var channels = discordCommandContext.Guild.Channels;
                var foundChannels = channels.Where(x => string.Equals(value, x.Name, StringComparison.OrdinalIgnoreCase));

                if (foundChannels.Count() > 1)
                    return TypeParserResult<T>.Unsuccessful(
                        "Channel name is ambiguous. Try mentioning the channel or enter the id.");
                
                channel = foundChannels.FirstOrDefault();
            }


            return channel is T tChannel
                ? TypeParserResult<T>.Successful(tChannel)
                : TypeParserResult<T>.Unsuccessful("Channel hasn't been found.");
        }
    }
}