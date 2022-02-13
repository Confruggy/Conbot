using System.Threading.Tasks;

using Conbot.Commands;

using Disqord;
using Disqord.Bot;

using Qmmands;

namespace Conbot.WelcomePlugin;

[Name("Welcome & Goodbye Messages")]
[Description("Configures welcome and goodbye messages.")]
[Commands.RequireGuild]
public class WelcomeAndGoodbyeModule : ConbotGuildModuleBase
{
    [Group("welcome")]
    [Description("Configures welcome messages.")]
    public class WelcomeModule : ConbotGuildModuleBase
    {
        private readonly WelcomeContext _db;

        public WelcomeModule(WelcomeContext db) => _db = db;

        [Command("toggle", "")]
        [Description("Toggles welcome messages.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
        public async Task<DiscordCommandResult> ToggleAsync(
            [Description("Whether to enable or disable welcome messages.")]
            [Choices("enable", "disable")]
            string toggle)
        {
            var config = await _db.GetOrCreateConfigurationAsync(Context.Guild);

            string? reply = null;

            switch (toggle)
            {
                case "enable":
                    if (config.ShowWelcomeMessages == true)
                        return Fail("Welcome messages are already enabled.");

                    config.ShowWelcomeMessages = true;
                    reply = "Welcome messages have been enabled.";

                    break;
                case "disable":
                    if (config.ShowWelcomeMessages == false)
                        return Fail("Welcome messages are already disabled.");

                    config.ShowWelcomeMessages = false;
                    reply = "Welcome messages have been disabled.";

                    break;
            }

            return Reply(reply).RunWith(_db.SaveChangesAsync());
        }

        [Command("template")]
        [Description("Sets the template for the message when someone joins the server.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
        public async Task<DiscordCommandResult> TemplateAsync(
            [Description("The template to set.")]
            [Remainder]
            string template)
        {
            var config = await _db.GetOrCreateConfigurationAsync(Context.Guild);

            config.WelcomeMessageTemplate = template;

            return Reply("Welcome message template has been set.").RunWith(_db.SaveChangesAsync());
        }

        [Command("channel")]
        [Description("Sets the channel where welcome messages will be sent.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
        public async Task<DiscordCommandResult> ChannelAsync(
            [Description("The channel to set.")]
            [Remarks("If left blank, welcome messages will be sent in the system messages channel.")]
            [Remainder]
            ITextChannel? channel)
        {
            var config = await _db.GetOrCreateConfigurationAsync(Context.Guild);

            string reply;

            if (channel is not null)
            {
                config.WelcomeChannelId = channel.Id;
                reply = $"Channel for welcome messages has been set to {channel.Mention}.";
            }
            else
            {
                config.WelcomeChannelId = null;
                reply = $"Channel for welcome messages has been set to the system messages channel.";
            }

            return Reply(reply).RunWith(_db.SaveChangesAsync());
        }

        [Command("settings")]
        [Description("Shows the current settings for welcome messages.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
        [Commands.RequireBotChannelPermissions(Permission.SendEmbeds)]
        public async Task<DiscordCommandResult> SettingsAsync()
        {
            var config = await _db.GetConfigurationAsync(Context.Guild);

            bool enabled = config?.ShowWelcomeMessages ?? false;
            string channel = config?.WelcomeChannelId is not null
                ? Mention.Channel(config.WelcomeChannelId.Value)
                : Mention.Channel(Context.Guild.SystemChannelId ?? 0);
            string template = config?.WelcomeMessageTemplate ?? WelcomeService.DefaultWelcomeMessageTemplate;

            var embed = new SettingsEmbedBuilder(Context)
                .WithTitle("Welcome Messages Settings")
                .WithGuild(Context.Guild)
                .AddSetting("Show Welcome Messages", enabled, "toggle")
                .AddSetting("Welcome Messages Channel", channel, "channel")
                .AddSetting("Template", template, "template")
                .Build();

            return Reply(embed);
        }
    }

    [Group("goodbye")]
    [Description("Configures goodbye messages.")]
    public class GoodbyeModule : ConbotGuildModuleBase
    {
        private readonly WelcomeContext _db;

        public GoodbyeModule(WelcomeContext db) => _db = db;

        [Command("toggle", "")]
        [Description("Toggles goodbye messages.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
        public async Task<DiscordCommandResult> ToggleAsync(
            [Description("Whether to enable or disable goodbye messages.")]
            [Choices("enable", "disable")]
            string toggle)
        {
            var config = await _db.GetOrCreateConfigurationAsync(Context.Guild);

            string? reply = null;

            switch (toggle)
            {
                case "enable":
                    if (config.ShowGoodbyeMessages == true)
                        return Fail("Goodbye messages are already enabled.");

                    config.ShowGoodbyeMessages = true;
                    reply = "Goodbye messages have been enabled.";

                    break;
                case "disable":
                    if (config.ShowGoodbyeMessages == false)
                        return Fail("Goodbye messages are already disabled.");

                    config.ShowGoodbyeMessages = false;
                    reply = "Goodbye messages have been disabled.";

                    break;
            }

            return Reply(reply).RunWith(_db.SaveChangesAsync());
        }

        [Command("template", "message")]
        [Description("Sets the template for the message when someone leaves the server.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
        public async Task<DiscordCommandResult> TemplateAsync(
            [Description("The template to set.")]
            [Remainder]
            string template)
        {
            var config = await _db.GetOrCreateConfigurationAsync(Context.Guild);

            config.GoodbyeMessageTemplate = template;

            return Reply("Goodbye message template has been set.").RunWith(_db.SaveChangesAsync());
        }

        [Command("channel")]
        [Description("Sets the channel where goodbye messages will be sent.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
        public async Task<DiscordCommandResult> ChannelAsync(
            [Description("The channel to set.")]
            [Remarks("If left blank, goodbye messages will be sent in the system messages channel.")]
            [Remainder]
            ITextChannel? channel)
        {
            var config = await _db.GetOrCreateConfigurationAsync(Context.Guild);

            string reply;

            if (channel is not null)
            {
                config.GoodbyeChannelId = channel.Id;
                reply = $"Channel for goodbye messages has been set to {channel.Mention}.";
            }
            else
            {
                config.WelcomeChannelId = null;
                reply = $"Channel for goodbye messages has been set to the system messages channel.";
            }

            return Reply(reply).RunWith(_db.SaveChangesAsync());
        }

        [Command("settings")]
        [Description("Shows the current settings for goodbye messages.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
        [Commands.RequireBotChannelPermissions(Permission.SendEmbeds)]
        public async Task<DiscordCommandResult> SettingsAsync()
        {
            var config = await _db.GetConfigurationAsync(Context.Guild);

            bool enabled = config?.ShowGoodbyeMessages ?? false;
            string channel = config?.GoodbyeChannelId is not null
                ? Mention.Channel(config.GoodbyeChannelId.Value)
                : Mention.Channel(Context.Guild.SystemChannelId ?? 0);
            string template = config?.GoodbyeMessageTemplate is not null
                ? config.GoodbyeMessageTemplate
                : WelcomeService.DefaultGoodbyeMessageTemplate;

            var embed = new SettingsEmbedBuilder(Context)
                .WithTitle("Goodbye Messages Settings")
                .WithGuild(Context.Guild)
                .AddSetting("Show Goodbye Messages", enabled, "toggle")
                .AddSetting("Goodbye Messages Channel", channel, "channel")
                .AddSetting("Template", template, "template")
                .Build();

            return Reply(embed);
        }
    }
}