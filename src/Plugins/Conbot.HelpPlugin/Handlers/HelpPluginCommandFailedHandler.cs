using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Interactive;

using Disqord;
using Disqord.Gateway;

using Qmmands;

namespace Conbot.HelpPlugin
{
    public class HelpPluginCommandFailedHandler : ICommandFailedHandler
    {
        private readonly IConfiguration _config;
        private readonly InteractiveService _interactiveService;
        private readonly HelpService _helpService;

        public HelpPluginCommandFailedHandler(IConfiguration config, InteractiveService interactiveService,
            HelpService helpService)
        {
            _config = config;
            _interactiveService = interactiveService;
            _helpService = helpService;
        }

        public async ValueTask HandleFailedResultAsync(ConbotCommandContext context, FailedResult result)
        {
            var message = FormatFailureMessage(context, result);

            if (message is null)
                return;

            if (context.GuildId is not null)
            {
                var user = context.Bot.GetMember(context.GuildId.Value, context.Bot.CurrentUser.Id);
                var channel = context.Bot.GetChannel(context.GuildId.Value, context.ChannelId);

                if (!user.GetPermissions(channel).Has(Permission.AddReactions))
                    return;
            }

            var command = result switch
            {
                ArgumentParseFailedResult argumentParseFailedResult => argumentParseFailedResult.Command,
                TypeParseFailedResult typeParseFailedResult => typeParseFailedResult.Parameter.Command,
                ChecksFailedResult checksFailedResult => checksFailedResult.Command,
                ParameterChecksFailedResult parameterChecksFailedResult => parameterChecksFailedResult.Parameter.Command,
                RuntimeFailedResult runtimeFailedResult => runtimeFailedResult.Command,
                _ => null
            };

            Module? module = null;

            if (command is null)
            {
                if (result is OverloadsFailedResult overloadsFailedResult)
                {
                    if (overloadsFailedResult.FailedOverloads.Count == 1)
                        command = overloadsFailedResult.FailedOverloads.First().Key;
                    else
                        module = overloadsFailedResult.FailedOverloads.First().Key.Module;
                }
                else
                {
                    return;
                }
            }

            bool executeHelpCommand = false;

            message
                .WithPrecondition(x => x.Id == context.Author.Id)
                .AddReactionCallback(_config.GetValue<string>("Emotes:Info"), x => x
                    .WithCallback((msg, _) =>
                    {
                        executeHelpCommand = true;
                        msg.Stop();
                    }));

            await _interactiveService.ExecuteInteractiveMessageAsync(message, context);

            if (executeHelpCommand)
            {
                if (command is not null)
                    await _helpService.ExecuteHelpMessageAsync(context, command, true);
                else if (module is not null)
                    await _helpService.ExecuteHelpMessageAsync(context, module, true);
            }
        }

        private static LocalInteractiveMessage? FormatFailureMessage(ConbotCommandContext context, FailedResult result)
        {
            string reason = context.Bot.FormatFailureReason(context, result);

            if (string.IsNullOrEmpty(reason))
                return null;

            var message = new LocalInteractiveMessage()
                .WithContent(reason);

            if (result is OverloadsFailedResult overloadsFailedResult)
            {
                var embed = new LocalEmbed()
                    .WithColor(0xED4245);

                foreach (var (overload, overloadResult) in overloadsFailedResult.FailedOverloads)
                {
                    string overloadReason = context.Bot.FormatFailureReason(context, overloadResult);
                    if (overloadReason == null)
                        continue;

                    embed.AddField($"{overload.FullAliases[0]} {HelpUtils.FormatParameters(overload)}", overloadReason);
                }

                message.AddEmbed(embed);
            }

            return message;
        }
    }
}
