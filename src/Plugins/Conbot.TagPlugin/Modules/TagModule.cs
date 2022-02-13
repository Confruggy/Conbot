using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;

using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;

using Humanizer;

using Qmmands;

using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Conbot.TagPlugin;

[Name("Tags"), Group("tag")]
public class TagModule : ConbotGuildModuleBase
{
    private readonly TagContext _db;
    private readonly IConfiguration _config;

    public TagModule(TagContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [Command("show", "")]
    [Description("Shows a tag.")]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public Task<DiscordCommandResult> TagAsync([Remainder, Description("The name of the tag.")] string name)
        => ShowTagAsync(name);

    [Command("raw")]
    [Description("Shows the raw content of a tag.")]
    [Remarks(
        "This escapes certain Markdownting from the content, " +
        "so you see the content like you would've typed it in. " +
        "This is useful for editing tags.")]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public Task<DiscordCommandResult> RawAsync([Remainder, Description("The name of the tag.")] string name)
        => ShowTagAsync(name, true);

    private async Task<DiscordCommandResult> ShowTagAsync(string name, bool raw = false)
    {
        var tag = await _db.GetTagAsync(Context.Guild, name);

        TagAlias? alias = null;

        if (tag is null)
        {
            alias = await _db.GetTagAliasAsync(Context.Guild, name);
            if (alias is null)
                return Fail($"Tag **{Markdown.Escape(name)}** hasn't been found.");

            tag = await _db.GetTagAsync(alias.TagId);
            if (tag is null) //Should never happen
                return Fail($"Tag **{Markdown.Escape(name)}** hasn't been found.");
        }

        await _db.AddTagUseAsync(tag, Context.Channel, Context.Message, alias);

        string content = raw ? Markdown.Escape(tag.Content) : tag.Content;

        return Reply(content).RunWith(_db.SaveChangesAsync());
    }

    [Command("create", "add")]
    [Description("Creates a tag.")]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> CreateAsync(
        [Description("The name of the tag."), NotEmpty, MaxLength(50), Inline]
        string name,
        [Remainder, Description("The content of the tag.")]
        string content)
    {
        name = name.TrimEnd();

        if (await _db.GetTagAsync(Context.Guild, name) is not null ||
            await _db.GetTagAliasAsync(Context.Guild, name) is not null)
        {
            return Fail($"Tag **{Markdown.Escape(name)}** already exists.");
        }

        await _db.CreateTagAsync(Context.Channel, Context.Message, name, content);

        return Reply($"Tag **{Markdown.Escape(name)}** has been created.").RunWith(_db.SaveChangesAsync());
    }

    [Command("delete", "remove")]
    [Description("Deletes a tag or an alias.")]
    [Remarks("Only the owner of a tag or a member with **Manage Server** permission can delete the tag.")]
    [Commands.RequireBotChannelPermissions(Permission.AddReactions | Permission.UseExternalEmojis)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> DeleteAsync(
        [Remainder, Description("The name of the tag to delete.")]
        string name)
    {
        var user = Context.Author;

        TagAlias? alias = null;
        var tag = await _db.GetTagAsync(Context.Guild, name);

        if (tag is null)
        {
            alias = await _db.GetTagAliasAsync(Context.Guild, name);

            if (alias is null)
                return Fail($"Tag **{Markdown.Escape(name)}** hasn't been found.");
        }

        var creator = await Context.Guild.FetchMemberAsync(tag?.OwnerId ?? alias?.OwnerId ?? 0);

        if (!(tag?.OwnerId == user.Id ||
              alias?.OwnerId == user.Id ||
              (user.GetPermissions().ManageMessages &&
               (creator is null || user.GetHierarchy() > creator.GetHierarchy()))))
        {
            return Fail("You aren't the owner of this tag.");
        }

        var prompt = Prompt("Do you really want to delete this tag?");
        var message = await prompt;

        string text;

        if (prompt.Result == true)
        {
            if (tag is not null)
            {
                _db.RemoveTag(tag);
                text = $"Tag **{Markdown.Escape(tag.Name)}** has been deleted.";
            }
            else
            {
                _db.RemoveTagAlias(alias!);
                text = $"Tag **{Markdown.Escape(alias!.Name)}** has been deleted.";
            }
        }
        else
        {
            if (tag is not null)
                text = $"Tag **{Markdown.Escape(tag.Name)}** hasn't been deleted.";
            else
                text = $"Alias **{Markdown.Escape(alias!.Name)}** hasn't been deleted.";
        }

        return Modify(message, text).RunWith(_db.SaveChangesAsync());
    }

    [Command("edit", "modify")]
    [Description("Edits the content of a tag you own.")]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> EditAsync(
        [Description("The name of the tag you want to edit.")]
        string name,
        [Remainder, Name("new content"), Description("The new content of the tag.")]
        string newContent)
    {
        var tag = await _db.GetTagAsync(Context.Guild, name);
        if (tag is null)
            return Fail($"Tag **{Markdown.Escape(name)}** hasn't been found.");

        if (tag.OwnerId != Context.Author.Id)
            return Fail("You aren't the owner of this tag.");

        await _db.ModifyTagAsync(tag, Context.Channel, Context.Message, newContent);

        return Reply($"Tag **{Markdown.Escape(tag.Name)}** has been edited.").RunWith(_db.SaveChangesAsync());
    }

    [Command("transfer")]
    [Description("Transfers a tag or alias you own to another member.")]
    [Remarks(
        "This will only change the owner of a tag or alias, but not the creator. However, only the owner " +
        "of a tag or alias is able to edit or delete it.")]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> TransferAsync(
        [Description("The name of the tag or alias you want to transfer.")]
        string name,
        [Description("The member you want to transfer the tag or alias to.")]
        IMember member)
    {
        if (member.Id == Context.Author.Id)
            return Fail("You can't transfer tags or aliases to yourself.");

        if (member.IsBot)
            return Fail("You can't transfer tags or aliases to bots.");

        TagAlias? alias = null;
        var tag = await _db.GetTagAsync(Context.Guild, name);

        if (tag is null)
        {
            alias = await _db.GetTagAliasAsync(Context.Guild, name);
            if (alias is null)
                return Fail($"Tag **{Markdown.Escape(name)}** hasn't been found.");
        }

        if (!(tag?.OwnerId == Context.Author.Id || alias?.OwnerId == Context.Author.Id))
            return Fail("You aren't the owner of this tag.");

        if (tag is not null)
        {
            await _db.ChangeTagOwnerAsync(tag, Context.Channel, Context.Message, member.Id,
                OwnerChangeType.Transfer);

            return Reply($"Tag **{Markdown.Escape(tag.Name)}** has been transferred to {member.Mention}.")
                .RunWith(_db.SaveChangesAsync());
        }

        if (alias is null)
            return Reply("lol"); //todo

        await _db.ChangeTagAliasOwnerAsync(alias, Context.Channel, Context.Message,
            member.Id, OwnerChangeType.Transfer);

        return Reply($"Alias **{Markdown.Escape(alias.Name)}** has been transferred to {member.Mention}.")
            .RunWith(_db.SaveChangesAsync());
    }

    [Command("claim", "yoink")]
    [Description("Claims a tag or alias from someone who isn't in the server anymore.")]
    [Remarks(
        "If a member created tags or aliases and left the server, you can claim them by using this command. This " +
        "will make you the new owner of the tag or alias.")]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> ClaimAsync(
        [Description("The name of the tag or alias you want to claim."), Remainder]
        string name)
    {
        var tag = await _db.GetTagAsync(Context.Guild, name);
        if (tag is not null)
        {
            if (tag.OwnerId == Context.Author.Id)
                return Fail("You already own this tag.");

            var user = await Context.Guild.FetchMemberAsync(tag.OwnerId);

            if (user is not null)
                return Fail("The owner of this tag is still in the server.");

            await _db.ChangeTagOwnerAsync(tag, Context.Channel, Context.Message,
                Context.Author.Id, OwnerChangeType.Claim);

            return Reply($"You claimed the tag **{Markdown.Escape(tag.Name)}**.").RunWith(_db.SaveChangesAsync());
        }
        else
        {
            var alias = await _db.GetTagAliasAsync(Context.Guild, name);
            if (alias is null)
                return Fail($"Tag or alias **{Markdown.Escape(name)}** hasn't been found.");

            if (alias.OwnerId == Context.Author.Id)
                return Fail("You already own this alias.");

            var user = await Context.Guild.FetchMemberAsync(alias.OwnerId);

            if (user is not null)
                return Fail("The owner of this alias is still in the server.");

            await _db.ChangeTagAliasOwnerAsync(alias, Context.Channel, Context.Message, Context.Author.Id,
                OwnerChangeType.Transfer);

            return Reply($"You claimed the alias **{Markdown.Escape(alias.Name)}**.")
                .RunWith(_db.SaveChangesAsync());
        }
    }

    [Command("info")]
    [Description("Shows information about a tag or an alias.")]
    [Commands.RequireBotChannelPermissions(Permission.SendEmbeds)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> InfoAsync(
        [Remainder, Description("The name of the tag or alias.")]
        string name)
    {
        var tags = await _db.GetTagsAsync(Context.Guild).ToArrayAsync();
        var tag = Array.Find(tags, x =>
            x.GuildId == Context.Guild.Id &&
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        if (tag is null)
        {
            var alias = await _db.GetTagAliasAsync(Context.Guild, name);

            if (alias is null)
                return Fail($"Tag **{Markdown.Escape(name)}** hasn't been found.");

            return Reply(CreateTagAliasEmbed(alias));
        }

        int count = tags.Length;
        int uses = tag.Uses.Count;
        int rank = count - tags.Count(x => x.Uses.Count <= uses) + 1;
        return Reply(CreateTagEmbed(tag, uses, rank, count));
    }

    private LocalEmbed CreateTagEmbed(Tag tag, int uses, int rank, int count)
    {
        var owner = Context.Guild.GetMember(tag.OwnerId);
        double days = (DateTime.UtcNow - tag.CreatedAt).TotalDays;
        double average = days > 1 ? Math.Round(uses / days) : uses;

        var createdAtText = new StringBuilder()
            .AppendLine(
                Markdown.Link(
                    Markdown.Timestamp(tag.CreatedAt, Markdown.TimestampFormat.RelativeTime),
                    tag.CreationUrl));

        if (tag.CreatorId != tag.OwnerId)
        {
            var creator = Context.Guild.GetMember(tag.CreatorId);

            createdAtText
                .Append("by ")
                .Append(creator?.Mention ?? "Member Not Found");
        }

        var modifiedAtText = new StringBuilder();
        var modification = tag.Modifications.OrderByDescending(x => x.ModifiedAt).FirstOrDefault();

        if (modification is null)
        {
            modifiedAtText.Append("Never");
        }
        else
        {
            modifiedAtText
                .AppendLine(
                    Markdown.Link(
                        Markdown.Timestamp(modification.ModifiedAt, Markdown.TimestampFormat.RelativeTime),
                        modification.Url));

            if (modification.UserId != tag.OwnerId)
            {
                var modificationUser = Context.Guild.GetMember(modification.UserId);

                modifiedAtText
                    .Append("by ")
                    .Append(modificationUser?.Mention ?? "Member Not Found");
            }
        }

        return new LocalEmbed()
            .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
            .WithAuthor(owner?.ToString(), owner?.GetAvatarUrl())
            .WithTitle(tag.Name)
            .AddField("Owner", owner?.Mention ?? "Member Not Found", true)
            .AddField("Uses", $"{uses:n0} ({average}/day)", true)
            .AddField("Rank", $"{rank:n0}/{count:n0}", true)
            .AddField("Created", createdAtText.ToString(), true)
            .AddField("Last Edited", modifiedAtText.ToString(), true);
    }

    private LocalEmbed CreateTagAliasEmbed(TagAlias alias)
    {
        var owner = Context.Guild.GetMember(alias.OwnerId);
        double days = (DateTime.UtcNow - alias.CreatedAt).TotalDays;
        int uses = alias.TagUses.Count;
        double average = days > 1 ? Math.Round(uses / days) : uses;

        var createdAtText = new StringBuilder()
            .AppendLine(
                Markdown.Link(
                    Markdown.Timestamp(alias.CreatedAt, Markdown.TimestampFormat.RelativeTime),
                    alias.CreationUrl));

        if (alias.CreatorId != alias.OwnerId)
        {
            var creator = Context.Guild.GetMember(alias.CreatorId);

            createdAtText
                .Append("by ")
                .Append(creator?.Mention ?? "Member Not Found");
        }

        return new LocalEmbed()
            .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
            .WithAuthor(owner?.ToString(), owner?.GetAvatarUrl())
            .WithTitle(alias.Name)
            .AddField("Owner", owner?.Mention ?? "Member Not Found", true)
            .AddField("Original Tag", alias.Tag.Name, true)
            .AddField("Uses", $"{uses:n0} ({average}/day)", true)
            .AddField("Created", createdAtText.ToString(), true);
    }

    [Command("alias")]
    [Description("Creates an alias for an already existing tag.")]
    [Remarks("When the original tag gets deleted, the alias gets deleted as well.")]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> AliasAsync(
        [Description("The name of the alias."), NotEmpty, MaxLength(50), Inline]
        string name,
        [Remainder, Name("tag name"), Description("The name of the tag the alias points to.")]
        string tagName)
    {
        name = name.TrimEnd();

        var tag = await _db.GetTagAsync(Context.Guild, tagName);
        if (tag is null)
            return Fail($"Tag **{Markdown.Escape(tagName)}** hasn't been found.");

        if (await _db.GetTagAsync(Context.Guild, name) is not null ||
            await _db.GetTagAliasAsync(Context.Guild, name) is not null)
        {
            return Fail($"Tag **{Markdown.Escape(name)}** already exists.");
        }

        await _db.CreateTagAliasAsync(tag, Context.Channel, Context.Message, name);

        return Reply(
                $"Alias **{Markdown.Escape(name)}** which points " +
                $"to **{Markdown.Escape(tag.Name)}** has been created.")
            .RunWith(_db.SaveChangesAsync());
    }

    [Command("list", "all")]
    [Description("Lists all or someone's tags for a server.")]
    [Commands.RequireBotChannelPermissions(
        Permission.AddReactions |
        Permission.SendEmbeds |
        Permission.UseExternalEmojis)]
    public async Task<DiscordCommandResult> ListAsync(
        [Description(
            "The member to lists tags from. If no user is entered, it lists all tags for the server instead.")]
        IMember? member = null,
        [Description("The page to start with.")]
        int page = 1)
    {
        var tags = await _db.GetTagsAsync(Context.Guild, member).OrderBy(x => x.Name).ToArrayAsync();

        if (tags.Length == 0)
        {
            if (member is null)
                return Reply("There aren't any tags for this server.");

            if (member.Id == Context.Author.Id)
                return Reply("You don't have any tags.");

            return Reply($"**{member.Nick ?? member.Name}** doesn't have any tags.");
        }

        int count = tags.Length;
        int padding = count.ToString().Length;
        var pageDescriptions = new List<string>();

        int i = 1;
        var pageText = new StringBuilder();

        foreach (var tag in tags)
        {
            pageText
                .Append('`')
                .Append(i.ToString().PadLeft(padding))
                .Append(".` ")
                .AppendLine(Markdown.Escape(tag.Name));

            if (i % 15 == 0 || i == count)
            {
                pageDescriptions.Add(pageText.ToString());
                pageText.Clear();
            }

            i++;
        }

        if (page > pageDescriptions.Count || page < 1)
            return Fail("This page doesn't exist.");

        List<Page> pages = new();

        for (int j = 0; j < pageDescriptions.Count; j++)
        {
            var embed = new LocalEmbed()
                .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                .WithAuthor(
                    member?.Name ?? Context.Guild.Name,
                    member?.GetAvatarUrl() ?? Context.Guild.GetIconUrl())
                .WithTitle("Tags")
                .WithDescription(pageDescriptions[j])
                .WithFooter($"Page {j + 1}/{pageDescriptions.Count} ({"entry".ToQuantity(count)})");

            pages.Add(new Page().AddEmbed(embed));
        }

        return Paginate(pages, startIndex: page - 1);
    }
}