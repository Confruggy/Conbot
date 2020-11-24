using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Extensions;
using Conbot.InteractiveMessages;
using Conbot.Services.Interactive;
using Conbot.TimeZonePlugin.Extensions;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.Configuration;
using NodaTime;
using Qmmands;

namespace Conbot.TagPlugin
{
    [Name("Tags"), Group("tag")]
    public class TagModule : DiscordModuleBase
    {
        private readonly InteractiveService _interactiveService;
        private readonly TagContext _db;
        private readonly IConfiguration _config;

        public TagModule(InteractiveService interactiveSerivce, TagContext db, IConfiguration config)
        {
            _interactiveService = interactiveSerivce;
            _db = db;
            _config = config;
        }

        [Command, Priority(-1)]
        [Description("Shows a tag.")]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public Task TagAsync([Remainder, Description("The name of the tag.")] string name) => ShowTagAsync(name);

        [Command("raw")]
        [Description("Shows the raw content of a tag.")]
        [Remarks(
            "This escapes certain formatting from the content, " +
            "so you see the content like you would've typed it in. " +
            "This is useful for editing tags.")]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public Task RawAsync([Remainder, Description("The name of the tag.")] string name) => ShowTagAsync(name, true);

        private async Task ShowTagAsync(string name, bool raw = false)
        {
            var tag = await _db.GetTagAsync(Context.Guild, name);

            TagAlias alias = null;
            if (tag == null)
            {
                alias = await _db.GetTagAliasAsync(Context.Guild, name);
                if (alias == null)
                    return;

                tag = await _db.GetTagAsync(alias.TagId);
                if (tag == null) //Should never happen
                    return;
            }

            await _db.AddTagUseAsync(tag, Context.Message, alias);

            string content = raw ? Format.Sanitize(tag.Content) : tag.Content;

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                SendMessageAsync(content));
        }

        [Command("create", "add")]
        [Description("Creates a tag.")]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task CreateAsync(
            [Description("The name of the tag."), NotEmpty, MaxLength(50)] string name,
            [Remainder, Description("The content of the tag.")] string content)
        {
            name = name.TrimEnd();

            if (await _db.GetTagAsync(Context.Guild, name) != null ||
                await _db.GetTagAliasAsync(Context.Guild, name) != null)
            {
                await ReplyAsync($"Tag **{Format.Sanitize(name)}** already exists.");
                return;
            }

            await _db.CreateTagAsync(Context.Message, name, content);

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync($"Tag **{Format.Sanitize(name)}** has been created."));
        }

        [Command("delete", "remove")]
        [Description("Deletes a tag or an alias.")]
        [Remarks("Only the owner of a tag or a member with **Manage Server** permission can delete the tag.")]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task DeleteAsync([Remainder, Description("The name of the tag to delete.")] string name)
        {
            var user = (SocketGuildUser)Context.User;

            TagAlias alias = null;
            var tag = await _db.GetTagAsync(Context.Guild, name);

            if (tag == null)
            {
                alias = await _db.GetTagAliasAsync(Context.Guild, name);
                if (alias == null)
                {
                    await ReplyAsync($"Tag **{Format.Sanitize(name)}** hasn't been found.");
                    return;
                }
            }

            var creator = Context.Guild.GetUser(tag?.OwnerId ?? alias?.OwnerId ?? 0);

            if (!(tag?.OwnerId == user.Id ||
                alias?.OwnerId == user.Id ||
                (user.GuildPermissions.ManageMessages &&
                (creator == null || user.Hierarchy > creator.Hierarchy))))
            {
                await ReplyAsync("You aren't the owner of this tag.");
                return;
            }

            var message = await ReplyAsync("Do you really want to delete this tag?");

            var interactiveMessage = new InteractiveMessageBuilder()
                .WithPrecondition(x => x.Id == Context.User.Id)
                .AddReactionCallback((Func<ReactionCallbackBuilder, ReactionCallbackBuilder>)(x => x
                    .WithEmote("greentick:314068319902760970")
                    .WithCallback((Func<IReaction, Task>)(async _ =>
                    {
                        if (tag != null)
                            _db.RemoveTag(tag);
                        else _db.RemoveTagAlias(alias);

                        await Task.WhenAll(
                            _db.SaveChangesAsync(),
                            base.ReplyAsync((string)$"Tag **{Format.Sanitize((string)tag.Name)}** has been deleted."),
                            DeletableExtensions.TryDeleteAsync(message));
                    }))))
                .AddReactionCallback((Func<ReactionCallbackBuilder, ReactionCallbackBuilder>)(x => x
                    .WithEmote("redtick:314068319986647050")
                    .WithCallback((Func<IReaction, Task>)(async _ =>
                    {
                        await Task.WhenAll(
                            base.ReplyAsync((string)$"Tag **{Format.Sanitize((string)tag.Name)}** hasn't been deleted."),
                            DeletableExtensions.TryDeleteAsync(message));
                    }))))
                .Build();

            await _interactiveService.ExecuteInteractiveMessageAsync(interactiveMessage, message, Context.User);
        }

        [Command("edit", "modify")]
        [Description("Edits the content of a tag you own.")]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task EditAsync(
            [Description("The name of the tag you want to edit.")] string name,
            [Remainder, Name("new content"), Description("The new content of the tag.")] string newContent)
        {
            var tag = await _db.GetTagAsync(Context.Guild, name);
            if (tag == null)
            {
                await ReplyAsync($"Tag **{Format.Sanitize(name)}** hasn't been found.");
                return;
            }

            if (tag.OwnerId != Context.User.Id)
            {
                await ReplyAsync("You aren't the owner of this tag.");
                return;
            }

            await _db.ModifyTagAsync(tag, Context.Message, newContent);

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync($"Tag **{Format.Sanitize(tag.Name)}** has been edited."));
        }

        [Command("info")]
        [Description("Shows information about a tag or an alias.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task InfoAsync([Remainder, Description("The name of the tag or alias.")] string name)
        {
            var tags = await _db.GetTagsAsync(Context.Guild);
            var tag = tags.Find(
                x => x.GuildId == Context.Guild.Id && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            var timeZone = await Context.GetUserTimeZoneAsync();

            if (tag == null)
            {
                var alias = await _db.GetTagAliasAsync(Context.Guild, name);

                if (alias == null)
                {
                    await ReplyAsync($"Tag **{Format.Sanitize(name)}** hasn't been found.");
                    return;
                }

                await ReplyAsync("", embed: CreateTagAliasEmbed(alias, timeZone));
            }
            else
            {
                int count = tags.Count;
                int uses = tag.Uses.Count;
                int rank = count - tags.Count(x => x.Uses.Count <= uses) + 1;
                await ReplyAsync("", embed: CreateTagEmbed(tag, uses, rank, count, timeZone));
            }
        }

        private Embed CreateTagEmbed(Tag tag, int uses, int rank, int count, DateTimeZone timeZone)
        {
            double days = (Context.Message.Timestamp - tag.Creation.CreatedAt).TotalDays;
            double average = days > 1 ? Math.Round(uses / days) : uses;

            var modification = tag.Modifications.OrderByDescending(x => x.ModifiedAt).FirstOrDefault();

            var owner = Context.Guild.GetUser(tag.OwnerId);

            var createdAt = DateTimeToClickableString(
                Instant.FromDateTimeUtc(tag.Creation.CreatedAt).InZone(timeZone), tag.Creation.Url);

            var modifiedAt = modification == null
                ? "Never"
                : DateTimeToClickableString(
                    Instant.FromDateTimeUtc(modification.ModifiedAt).InZone(timeZone), modification.Url);

            return new EmbedBuilder()
                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                .WithAuthor(x => x.WithIconUrl(owner?.GetAvatarUrl()).WithName(owner?.ToString()))
                .WithTitle(tag.Name)
                .AddField("Owner", owner?.Mention ?? "Member not found", true)
                .AddField("Uses", $"{uses:n0} ({average}/day)", true)
                .AddField("Rank", $"{rank:n0}/{count:n0}", true)
                .AddField("Created", createdAt, true)
                .AddField("Last edited", modifiedAt, true)
                .Build();
        }

        private Embed CreateTagAliasEmbed(TagAlias alias, DateTimeZone timeZone)
        {
            double days = (Context.Message.Timestamp - alias.Creation.CreatedAt).TotalDays;
            int uses = alias.TagUses.Count;
            double average = days > 1 ? Math.Round(uses / days) : uses;

            var owner = Context.Guild.GetUser(alias.OwnerId);

            var createdAt = DateTimeToClickableString(
                Instant.FromDateTimeUtc(alias.Creation.CreatedAt).InZone(timeZone), alias.Creation.Url);

            return new EmbedBuilder()
                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                .WithAuthor(owner?.ToString(), owner?.GetAvatarUrl())
                .WithTitle(alias.Name)
                .AddField("Creator", owner?.Mention ?? "Member not found", true)
                .AddField("Original Tag", alias.Tag.Name, true)
                .AddField("Uses", $"{uses:n0} ({average}/day)", true)
                .AddField("Created", createdAt, true)
                .Build();
        }

        public static string DateTimeToClickableString(ZonedDateTime date, string url)
            => $"[{date.ToReadableShortString()}]({url})";

        [Command("alias")]
        [Description("Creates an alias for an already existing tag.")]
        [Remarks("When the original tag gets deleted, the alias gets deleted aswell.")]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task AliasAsync(
            [Description("The name of the alias."), NotEmpty, MaxLength(50)] string name,
            [Remainder, Name("tag name"), Description("The name of the tag the alias points to.")] string tagName)
        {
            name = name.TrimEnd();

            var tag = await _db.GetTagAsync(Context.Guild, tagName);
            if (tag == null)
            {
                await ReplyAsync($"Tag **{Format.Sanitize(tagName)}** hasn't been found.");
                return;
            }

            if (await _db.GetTagAsync(Context.Guild, name) != null ||
                await _db.GetTagAliasAsync(Context.Guild, name) != null)
            {
                await ReplyAsync($"Tag **{Format.Sanitize(name)}** already exists.");
                return;
            }

            await _db.CreateTagAliasAsync(tag, Context.Message, name);

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync(
                    $"Alias **{Format.Sanitize(name)}** which points " +
                    $"to **{Format.Sanitize(tag.Name)}** has been created."));
        }

        [Command("all")]
        [Description("Lists all tags in this server.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.AddReactions)]
        public Task AllAsync([Description("The page to start with")] int page = 1)
            => ListAsync(page: page);

        [Command("list")]
        [Description("Lists all your or someone else's tags in this server.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.AddReactions)]
        public Task AllAsync(
            [Description("The member to lists tags from. If no user is entered, it lists your tags instead.")]
                IGuildUser user = null,
            [Description("The page to start with.")] int page = 1)
            => ListAsync(user ?? Context.User as IGuildUser, page);

        public async Task ListAsync(IGuildUser user = null, int page = 1)
        {
            var tags = (await _db.GetTagsAsync(Context.Guild, user)).OrderBy(x => x.Name);

            if (!tags.Any())
            {
                if (user == null)
                    await ReplyAsync("There aren't any tags for this server.");
                else if (user.Id == Context.User.Id)
                    await ReplyAsync("You don't have any tags.");
                else await ReplyAsync($"**{user.Nickname ?? user.Username}** doesn't have any tags.");
                return;
            }

            int count = tags.Count();
            int padding = count.ToString().Length;
            var pages = new List<string>();

            int i = 1;
            var pageText = new StringBuilder();

            foreach (var tag in tags)
            {
                pageText.Append('`')
                    .Append(i.ToString().PadLeft(padding))
                    .Append(".` ")
                    .AppendLine(Format.Sanitize(tag.Name));

                if (i % 15 == 0 || i == count)
                {
                    pages.Add(pageText.ToString());
                    pageText.Clear();
                }
                i++;
            }

            if (page > pages.Count || page < 1)
            {
                await ReplyAsync("This page doesn't exist.");
                return;
            }

            var paginator = new Paginator();

            for (int j = 0; j < pages.Count; j++)
            {
                var embed = new EmbedBuilder()
                    .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                    .WithAuthor(user?.Username ?? Context.Guild.Name, user?.GetAvatarUrl() ?? Context.Guild.IconUrl)
                    .WithTitle("Tags")
                    .WithDescription(pages[j])
                    .WithFooter($"Page {j + 1}/{pages.Count} ({"entry".ToQuantity(count)})")
                    .Build();
                paginator.AddPage(embed);
            }

            await paginator.RunAsync(_interactiveService, Context, page - 1);
        }
    }
}