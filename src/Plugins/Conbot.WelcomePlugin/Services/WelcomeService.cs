using System;
using System.Threading.Tasks;

using Disqord;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conbot.WelcomePlugin
{
    public class WelcomeService : DiscordBotService
    {
        private readonly ILogger<WelcomeService> _logger;
        private readonly DiscordBot _bot;
        private readonly HandlebarsService _handlebars;
        private readonly IServiceScopeFactory _scopeFactory;

        public const string DEFAULT_WELCOME_MESSAGE_TEMPLATE
            = "Welcome {{user.mention}} on **{{server.name}}**!";

        public const string DEFAULT_GOODBYE_MESSAGE_TEMPLATE
            = "Goodbye {{user}}. Sorry to see you leave!";

        public WelcomeService(DiscordBot bot, HandlebarsService handlebars, IServiceScopeFactory scopeFactory,
            ILogger<WelcomeService> logger)
        {
            _bot = bot;
            _handlebars = handlebars;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs e)
        {
            using var scope = _scopeFactory.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<WelcomeContext>();

            var config = await context.GetConfigurationAsync(e.Guild);

            if (config is null)
                return;

            if (!config.ShowWelcomeMessages)
                return;

            var member = e.Member;
            var guild = e.Guild;

            CachedMessageGuildChannel? channel = null;

            if (config.WelcomeChannelId is not null)
                channel = guild.GetChannel(config.WelcomeChannelId.Value) as CachedMessageGuildChannel;

            if (channel is null && guild.SystemChannelId is not null)
                channel = guild.GetChannel(guild.SystemChannelId.Value) as CachedMessageGuildChannel;

            if (channel is null)
                return;

            string templateSource = config.WelcomeMessageTemplate ?? DEFAULT_WELCOME_MESSAGE_TEMPLATE;

            var template = _handlebars.Compile(templateSource);

            var data = new
            {
                user = new TemplateUser(member),
                server = new TemplateGuild(guild),
                channel = new TemplateChannel(channel)
            };

            var message = new LocalMessage().WithContent(template(data));

            try
            {
                await channel.SendMessageAsync(message);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Sending welcome message for {Member} failed on {Guild}/{Channel}", member, guild, channel);
            }
        }

        protected override async ValueTask OnMemberLeft(MemberLeftEventArgs e)
        {
            using var scope = _scopeFactory.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<WelcomeContext>();

            var config = await context.GetConfigurationAsync(e.Guild);

            if (config is null)
                return;

            if (!config.ShowGoodbyeMessages)
                return;

            var user = e.User;
            var guild = e.Guild;

            CachedMessageGuildChannel? channel = null;

            if (config.GoodbyeChannelId is not null)
                channel = guild.GetChannel(config.GoodbyeChannelId.Value) as CachedMessageGuildChannel;

            if (channel is null && guild.SystemChannelId is not null)
                channel = guild.GetChannel(guild.SystemChannelId.Value) as CachedMessageGuildChannel;

            if (channel is null)
                return;

            string templateSource = config.GoodbyeMessageTemplate ?? DEFAULT_GOODBYE_MESSAGE_TEMPLATE;

            var template = _handlebars.Compile(templateSource);

            var data = new
            {
                user = new TemplateUser(user),
                server = new TemplateGuild(guild),
                channel = new TemplateChannel(channel)
            };

            var message = new LocalMessage().WithContent(template(data));

            try
            {
                await channel.SendMessageAsync(message);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Sending goodbye message for {User} failed on {Guild}/{Channel}", user, guild, channel);
            }
        }
    }
}
