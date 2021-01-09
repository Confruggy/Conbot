using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Conbot.Commands;

using Qmmands;

namespace Conbot.ReminderPlugin
{
    public class ReminderArgumentParser : IArgumentParser
    {
        public async ValueTask<ArgumentParserResult> ParseAsync(CommandContext context)
        {
            if (context is not DiscordCommandContext discordCommandContext)
                return ConbotArgumentParserResult.Failed("Invalid context.");

            if (discordCommandContext.Interaction != null || string.IsNullOrEmpty(context.RawArguments))
            {
                var commandService = context.ServiceProvider.GetRequiredService<CommandService>();
                return await commandService.DefaultArgumentParser.ParseAsync(context);
            }

            string? remainder = context.RawArguments;

            var dateRegex = new Regex("^ ?(?:(?:on(?: +the)? +)?(\\d+[\\/.-]\\d+(?:[\\/.-]\\d+)?|\\d+(?:[a-z]{2}|\\.)? +(?:jan(?:\\.|uary)?|feb(?:\\.|ruary)?|mar(?:\\.|ch)?|apr(?:\\.|il)?|may|jun(?:\\.|e)?|jul(?:\\.|y)?|aug(?:\\.|ust)?|sept(?:\\.|ember)?|oct(?:\\.|ober)?|nov(?:\\.|ember)?|dec(?:\\.|ember)?)(?: +(?!\\d+\\:)\\d+)?))|(?:(?:on +)?(?:(?:the +)?(next|last))? +)?(mon(?:\\.|day)?|tue(?:\\.|s(?:\\.|day)?)?|wed(?:\\.|day)?|thu(?:\\.|r(?:\\.|s(?:\\.|day)?)?)?|fri(?:\\.|day)?|sat(?:\\.|day)?|sun(?:\\.|day)?)");
            var timeRegex = new Regex("^ ?(?:(?:at +)?(?<time>\\d+\\:\\d+(?:\\:\\d+)?(?:(?: *[ap]m)?))|(?:at +)?(?<time>\\d+(?: *[ap]m))|(?:at +)(?<time>\\d+))");

            Match dateMatch;
            Match timeMatch;

            if ((dateMatch = dateRegex.Match(remainder.ToLowerInvariant())).Success)
            {
                remainder = remainder[dateMatch.Length..];
                timeMatch = timeRegex.Match(remainder.ToLowerInvariant());

                if (timeMatch.Success)
                    remainder = remainder[timeMatch.Length..];
            }
            else if ((timeMatch = timeRegex.Match(remainder.ToLowerInvariant())).Success)
            {
                remainder = remainder[timeMatch.Length..];
                dateMatch = dateRegex.Match(remainder.ToLowerInvariant());

                if (dateMatch.Success)
                    remainder = remainder[dateMatch.Length..];
            }

            if (!dateMatch.Success && !timeMatch.Success)
            {
                var durationRegex = new Regex("^ ?(?:in +)?(?:(-?\\d{1,9}) *y(?:ears?)?)?(?: *(-?\\d{1,9}) *mo(?:nths?)?)?(?: *(-?\\d{1,9}) *w(?:eeks?)?)?(?: *(-?\\d{1,9}) *d(?:ays?)?)?(?: *(-?\\d{1,9}) *h(?:ours?)?)?(?: *(-?\\d{1,9}) *m(?:inutes?)?)?(?: *(-?\\d{1,9}) *s(?:econds?)?)?");
                var durationMatch = durationRegex.Match(remainder.ToLowerInvariant());

                remainder = remainder[durationMatch.Length..];
            }

            if (string.IsNullOrWhiteSpace(remainder))
                remainder = null;

            if (remainder?.StartsWith(' ') == false)
                return ConbotArgumentParserResult.Failed("There must be a space between the time and the message.");

            var arguments = new Dictionary<Parameter, object?>
            {
                [context.Command.Parameters[0]] = context.RawArguments[..^(remainder?.Length ?? 0)],
                [context.Command.Parameters[1]] = remainder,
            };

            return ConbotArgumentParserResult.Successful(arguments);
        }
    }
}
