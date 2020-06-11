using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Services.Help;
using Discord;
using Qmmands;

namespace Conbot.Modules.Help
{
    [Name("Help")]
    [Group("help")]
    [Description("Gives information about commands.")]
    [RequireBotPermission(ChannelPermission.EmbedLinks|ChannelPermission.AddReactions)]
    public class HelpModule : DiscordModuleBase
    {
        private readonly HelpService _service;

        public HelpModule(HelpService service) => _service = service;

        [Command]
        [Description("Shows all available commands.")]
        public Task HelpAsync() => _service.ExecuteHelpMessageAsync(Context);

        [Command]
        [Description("Gives information about a specific module.")]
        [Priority(2)]
        public Task HelpAsync(
            [Remainder, Description("The module to give information about.")] Module module)
         => _service.ExecuteHelpMessageAsync(Context, startModule: module);

        [Command]
        [Description("Gives information about a specific command.")]
        [Priority(1)]
        public Task HelpAsync(
            [Remainder, Description("The command to give information about.")] Command command)
            => _service.ExecuteHelpMessageAsync(Context, startCommand: command);
    }
}