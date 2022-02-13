using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Disqord;
using Disqord.Gateway;
using Disqord.Rest;

using Qmmands;

using Qommon.Collections;

namespace Conbot.Commands;

public class MemberTypeParser : ConbotGuildTypeParser<IMember>
{
    public override async ValueTask<TypeParserResult<IMember>> ParseAsync(Parameter parameter, string value,
        ConbotGuildCommandContext context)
    {
        if (!context.Bot.CacheProvider.TryGetMembers(context.GuildId, out var memberCache))
            throw new InvalidOperationException($"The {GetType().Name} requires the member cache.");

        IMember? member;

        if (Snowflake.TryParse(value, out var id) || Mention.TryParseUser(value, out id))
        {
            if (!memberCache.TryGetValue(id, out var cachedMember))
            {
                await using (context.BeginYield())
                {
                    if (context.Bot.GetShard(context.GuildId).RateLimiter.GetRemainingRequests() < 3)
                    {
                        member = await context.Bot.FetchMemberAsync(context.GuildId, id)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        var members = await context.Bot.Chunker.QueryAsync(context.GuildId, new[] { id })
                            .ConfigureAwait(false);
                        member = members.GetValueOrDefault(id);
                    }
                }
            }
            else
            {
                member = cachedMember;
            }
        }
        else
        {
            string name;
            string? discriminator;
            int hashIndex = value.LastIndexOf('#');

            if (hashIndex != -1 && hashIndex + 5 == value.Length)
            {
                name = value[0..^5];
                discriminator = value[(hashIndex + 1)..];
            }
            else
            {
                name = value;
                discriminator = null;
            }

            static IMember? FindMember(IReadOnlyCollection<IMember> members, string name, string? discriminator)
            {
                if (discriminator is not null)
                {
                    return members.FirstOrDefault(x => x.Name == name && x.Discriminator == discriminator);
                }

                return members.FirstOrDefault(x => x.Name == name) ??
                       members.FirstOrDefault(x => x.Nick == name);
            }

            member = FindMember(memberCache.Values, name, discriminator);

            if (member == null)
            {
                await using (context.BeginYield())
                {
                    IReadOnlyCollection<IMember> members;
                    if (context.Bot.GetShard(context.GuildId).RateLimiter.GetRemainingRequests() < 3)
                    {
                        members = await context.Bot.SearchMembersAsync(context.GuildId, name)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        members = (await context.Bot.Chunker.QueryAsync(context.GuildId, name)
                            .ConfigureAwait(false)).Values.ToReadOnlyList();
                    }

                    member = FindMember(members, name, discriminator);
                }
            }
        }

        if (member is not null)
            return Success(member);

        return Failure("No member found matching the input.");
    }
}
