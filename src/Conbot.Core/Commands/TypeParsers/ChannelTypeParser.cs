using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Discord;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public class ChannelTypeParser<T> : TypeParser<T> where T : class, IChannel
    {
        public override ValueTask<TypeParserResult<T>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            var discordCommandContext = (DiscordCommandContext)context;

            if (discordCommandContext.Guild == null)
                return TypeParserResult<T>.Unsuccessful("This command must be used in a server.");

            IChannel? channel = null;

            if (MentionUtils.TryParseChannel(value, out ulong id))
            {
                channel = discordCommandContext.Guild.GetChannel(id);
            }
            else if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                channel = discordCommandContext.Guild.GetChannel(id);
            }
            else
            {
                var channels = discordCommandContext.Guild.Channels;
                var foundChannels = channels.Where(x => string.Equals(value, x.Name,
                    StringComparison.OrdinalIgnoreCase));

                if (foundChannels.Count() > 1)
                {
                    return TypeParserResult<T>.Unsuccessful(
                        "Channel name is ambiguous. Try mentioning the channel or enter the ID.");
                }

                channel = foundChannels.FirstOrDefault();
            }

            if (channel is T tChannel)
                return TypeParserResult<T>.Successful(tChannel);

            if (channel != null)
            {
                if (typeof(ITextChannel).IsAssignableFrom(typeof(T)))
                    return TypeParserResult<T>.Unsuccessful($"{parameter.Name.Humanize()} must be a text channel.");

                if (typeof(IVoiceChannel).IsAssignableFrom(typeof(T)))
                    return TypeParserResult<T>.Unsuccessful($"{parameter.Name.Humanize()} must be a voice channel.");

                if (typeof(ICategoryChannel).IsAssignableFrom(typeof(T)))
                    return TypeParserResult<T>.Unsuccessful($"{parameter.Name.Humanize()} must be a category.");
            }

            return TypeParserResult<T>.Unsuccessful("Channel wasn't found.");
        }
    }
}
