using System.Threading.Tasks;
using Conbot.Services.Help;
using Discord;
using Discord.Commands;

namespace Conbot.Modules.Help
{
    [Name("Help")]
    [Group("help")]
    [Summary("Gives information about commands.")]
    [RequireBotPermission(ChannelPermission.EmbedLinks)]
    [RequireBotPermission(ChannelPermission.AddReactions)]
    public class HelpModule : ModuleBase<ShardedCommandContext>
    {
        private readonly HelpService _service;

        public HelpModule(HelpService service) => _service = service;

        [Command]
        [Summary("Shows all available commands.")]
        public Task HelpAsync() => _service.ExecuteHelpMessageAsync(Context);

        [Command]
        [Summary("Gives more information about a specific module.")]
        [Priority(2)]
        public Task HelpAsync(
            [Remainder, Summary("The module to give more information about.")] ModuleInfo module)
         => _service.ExecuteHelpMessageAsync(Context, module);

        [Command]
        [Summary("Gives more information about a specific command.")]
        [Priority(1)]
        public Task HelpAsync(
            [Remainder, Summary("The command to give more information about.")] CommandInfo command)
            => _service.ExecuteHelpMessageAsync(Context, command);
    }
}