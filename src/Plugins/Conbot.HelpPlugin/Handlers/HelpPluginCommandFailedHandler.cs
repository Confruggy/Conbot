using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;

using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;

using Qmmands;

namespace Conbot.HelpPlugin;

public class HelpPluginCommandFailedHandler : ICommandFailedHandler
{
    private readonly IConfiguration _config;

    public HelpPluginCommandFailedHandler(IConfiguration config) => _config = config;

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

        if (command is null && result is OverloadsFailedResult overloadsFailedResult)
        {
            if (overloadsFailedResult.FailedOverloads.Count == 1)
                command = overloadsFailedResult.FailedOverloads.First().Key;
            else
                module = overloadsFailedResult.FailedOverloads.First().Key.Module;
        }

        ErrorView view;

        if (command is not null)
            view = new ErrorView(message, context, _config, command);
        else if (module is not null)
            view = new ErrorView(message, context, _config, module);
        else
            view = new ErrorView(message, context, _config);

        await context.Bot.RunMenuAsync(context.ChannelId, new DefaultMenu(view) { AuthorId = context.Author.Id });
    }

    private static LocalMessage? FormatFailureMessage(ConbotCommandContext context, FailedResult result)
    {
        string reason;
        Dictionary<string, string> fields = new();

        if (result is OverloadsFailedResult overloadsFailedResult)
        {
            if (overloadsFailedResult.FailedOverloads.Count == 1)
            {
                reason = ConbotBot.FormatFailureReason(context,
                    overloadsFailedResult.FailedOverloads.First().Value);
            }
            else
            {
                foreach (var (overload, overloadResult) in overloadsFailedResult.FailedOverloads)
                {
                    string overloadReason = ConbotBot.FormatFailureReason(context, overloadResult);

                    fields.Add($"{overload.FullAliases[0]} {HelpUtils.FormatParameters(overload)}", overloadReason);
                }

                reason = ConbotBot.FormatFailureReason(context, result);
            }
        }
        else
        {
            reason = ConbotBot.FormatFailureReason(context, result);
        }

        if (string.IsNullOrEmpty(reason))
            return null;

        var message = new LocalMessage()
            .WithContent(reason)
            .WithReply(context.Messages[^1].Id, context.ChannelId, context.GuildId);

        if (fields.Count <= 0)
        {
            return message;
        }

        var embed = new LocalEmbed()
            .WithColor(0xED4245);

        foreach ((string? key, string? value) in fields)
            embed.AddField(key, value);

        message.WithEmbeds(embed);

        return message;
    }
}