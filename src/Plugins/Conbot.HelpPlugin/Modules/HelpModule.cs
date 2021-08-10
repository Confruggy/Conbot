using Microsoft.Extensions.Configuration;

using Conbot.Commands;

using Disqord;
using Disqord.Bot;

using Qmmands;

namespace Conbot.HelpPlugin
{
    [Name("Help")]
    [Group("help")]
    [Description("Gives information about commands.")]
    [Commands.RequireBotChannelPermissions(
        Permission.AddReactions |
        Permission.SendEmbeds |
        Permission.UseExternalEmojis)]
    public class HelpModule : ConbotModuleBase
    {
        private readonly IConfiguration _config;

        public HelpModule(IConfiguration config) => _config = config;

        [Command("all", "")]
        [Description("Shows all available commands.")]
        public DiscordCommandResult Help() => View(new StartView(Context, _config));

        [Command("command", "")]
        [Description("Gives information about a specific command.")]
        [Priority(1)]
        public DiscordCommandResult Help(
            [Description("The command to give information about.")]
            [Remarks("Can be either the name of the command or any alias.")]
            [Remainder]
            Command command)
            => View(new CommandView(command, Context, _config));

        [Command("group", "")]
        [Description("Gives information about a specific group.")]
        [Priority(2)]
        public DiscordCommandResult Help(
            [Description("The group to give information about.")]
            [Remarks("Can be either the name of the group or the group's prefix.")]
            [Remainder]
            Module group)
            => View(new ModuleView(group, Context, _config));
    }
}
