using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Qmmands;

namespace Conbot.Commands
{
    public class UserTypeParser<T> : TypeParser<T> where T : class, IUser
    {
        public override async ValueTask<TypeParserResult<T>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            var discordCommandContext = context as DiscordCommandContext;

            IUser user = null;

            if (MentionUtils.TryParseUser(value, out var id))
                user = discordCommandContext.Guild?.GetUser(id)
                        ?? await discordCommandContext.Channel.GetUserAsync(id, CacheMode.CacheOnly);
            else if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                user = discordCommandContext.Guild?.GetUser(id)
                        ?? await discordCommandContext.Channel.GetUserAsync(id, CacheMode.CacheOnly);
            else
            {
                int index = value.LastIndexOf('#');

                if (index >= 0)
                {
                    string username = value.Substring(0, index);

                    if (ushort.TryParse(value.Substring(index + 1), out ushort discriminator))
                    {
                        if (discordCommandContext.Guild != null)
                        {
                            var guildUsers = discordCommandContext.Guild.Users;
                            user = guildUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                                string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
                        }
                        else
                        {
                            var channelUsers = await discordCommandContext.Channel.GetUsersAsync(CacheMode.CacheOnly)
                                .FlattenAsync();
                            user = channelUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                                string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                }
            };

            var tUser = user as T;

            return tUser != null
                ? TypeParserResult<T>.Successful(tUser)
                : TypeParserResult<T>.Unsuccessful("User hasn't been found.");
        }
    }
}