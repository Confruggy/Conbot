using System;

using Microsoft.Extensions.DependencyInjection;

using Disqord;
using Disqord.Bot;
using Disqord.Gateway;

namespace Conbot.Commands
{
    public class ConbotGuildCommandContext : ConbotCommandContext
    {
        public new virtual Snowflake GuildId => base.GuildId!.Value;

        public virtual CachedGuild Guild
        {
            get => _guild ??= Bot.GetGuild(GuildId);
            protected set => _guild = value;
        }

        private CachedGuild? _guild;

        public virtual CachedMember CurrentMember
        {
            get => _currentMember ??= Bot.GetMember(GuildId, Bot.CurrentUser.Id);
            protected set => _currentMember = value;
        }

        private CachedMember? _currentMember;

        public override IMember Author => (IMember)base.Author;

        public virtual CachedMessageGuildChannel Channel { get; }

        public ConbotGuildCommandContext(ConbotBot bot, IPrefix prefix, string input, IGatewayUserMessage message,
            CachedMessageGuildChannel channel, IServiceProvider services)
            : base(bot, prefix, input, message, services)
        {
            Channel = channel;
        }

        public ConbotGuildCommandContext(ConbotBot bot, IPrefix prefix, string input, IGatewayUserMessage message,
            CachedMessageGuildChannel channel, IServiceScope serviceScope)
            : base(bot, prefix, input, message, serviceScope)
        {
            Channel = channel;
        }
    }
}
