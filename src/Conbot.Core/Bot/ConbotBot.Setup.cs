using System.Threading;
using System.Threading.Tasks;

using Conbot.Commands;

using Disqord;
using Disqord.Bot.Parsers;

namespace Conbot
{
    public partial class ConbotBot
    {
        protected override ValueTask AddTypeParsersAsync(CancellationToken cancellationToken = default)
        {
            //Primitive
            Commands.AddTypeParser(new IntegerTypeParser(), true);
            Commands.AddTypeParser(new UnsignedLongTypeParser(), true);

            //Utils
            Commands.AddTypeParser(new TimeSpanTypeParser());
            Commands.AddTypeParser(new CommandTypeParser());
            Commands.AddTypeParser(new ModuleTypeParser());

            //Discord
            Commands.AddTypeParser(new SnowflakeTypeParser());
            Commands.AddTypeParser(new ColorTypeParser());
            Commands.AddTypeParser(new CustomEmojiTypeParser());
            Commands.AddTypeParser(new GuildEmojiTypeParser());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<IGuildChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<ICategorizableGuildChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<IMessageGuildChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<IVocalGuildChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<ITextChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<IVoiceChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<ICategoryChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<IStageChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<IThreadChannel>());
            Commands.AddTypeParser(new Commands.GuildChannelTypeParser<IStoreChannel>());
            Commands.AddTypeParser(new Commands.MemberTypeParser());
            Commands.AddTypeParser(new Commands.RoleTypeParser());

            return default;
        }

        protected override ValueTask AddModulesAsync(CancellationToken cancellationToken = default) => default;
    }
}
