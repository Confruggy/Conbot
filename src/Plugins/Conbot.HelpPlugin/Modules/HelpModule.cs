using System.Threading.Tasks;

using Conbot.Commands;

using Discord;

using Qmmands;

namespace Conbot.HelpPlugin
{
    [Name("Help")]
    [Group("help")]
    [Description("Gives information about commands.")]
    [RequireBotPermission(
        ChannelPermission.AddReactions |
        ChannelPermission.EmbedLinks |
        ChannelPermission.UseExternalEmojis)]
    public class HelpModule : DiscordModuleBase
    {
        private readonly HelpService _service;

        public HelpModule(HelpService service) => _service = service;

        [Command("all", "")]
        [Description("Shows all available commands.")]
        public Task HelpAsync() => _service.ExecuteHelpMessageAsync(Context);

        [Command("command", "")]
        [Description("Gives information about a specific command.")]
        [Priority(1)]
        public Task HelpAsync(
            [Description("The command to give information about.")]
            [Remarks("Can be either the name of the command or any alias.")]
            [Remainder]
            Command command)
            => _service.ExecuteHelpMessageAsync(Context, command);

        [Command("group", "")]
        [Description("Gives information about a specific group.")]
        [Priority(2)]
        public Task HelpAsync(
            [Description("The group to give information about.")]
            [Remarks("Can be either the name of the group or the group's prefix.")]
            [Remainder]
            Module group)
         => _service.ExecuteHelpMessageAsync(Context, group);
    }
}
