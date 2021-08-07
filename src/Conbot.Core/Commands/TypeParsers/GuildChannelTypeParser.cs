using System;
using System.Linq;
using System.Threading.Tasks;

using Disqord;
using Disqord.Gateway;

using Qmmands;

namespace Conbot.Commands
{
    public class GuildChannelTypeParser<TChannel> : ConbotGuildTypeParser<TChannel>
        where TChannel : class, IGuildChannel
    {
        private readonly string _channelString;

        public GuildChannelTypeParser()
        {
            var type = typeof(TChannel);
            _channelString = type != typeof(IGuildChannel) && type.IsInterface
                ? $"{type.Name[1..type.Name.IndexOf("Channel")].Replace("Guild", "").ToLower()} channel"
                : "channel";
        }

        public override ValueTask<TypeParserResult<TChannel>> ParseAsync(Parameter parameter, string value,
            ConbotGuildCommandContext context)
        {
            if (!context.Bot.CacheProvider.TryGetChannels(context.GuildId, out var channelCache))
                throw new InvalidOperationException($"The {GetType().Name} requires the channel cache.");

            var channels = channelCache.Values.OfType<TChannel>();

            TChannel? channel;

            if (Snowflake.TryParse(value, out var id) || Mention.TryParseChannel(value, out id))
                channel = channels.FirstOrDefault(x => x.Id == id);
            else
                channel = channels.FirstOrDefault(x => x.Name == value);

            if (channel != null)
                return Success(channel);

            return Failure($"No {_channelString} found matching the input.");
        }
    }
}
