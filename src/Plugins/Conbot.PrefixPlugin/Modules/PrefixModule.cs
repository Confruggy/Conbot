using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;

using Disqord;
using Disqord.Bot;

using Humanizer;

using Qmmands;

using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Conbot.PrefixPlugin;

[Name("Prefix")]
[Group("prefix")]
[Description("Sets up command prefixes for text commands.")]
[Remarks(
    "You can set up up to 10 prefixes for this server. " +
    "Commands can also be invoked by mentioning the bot or using Slash Commands. " +
    "Custom prefixes are only supported by text commands.")]
public class PrefixModule : ConbotGuildModuleBase
{
    private readonly PrefixContext _db;
    private readonly IConfiguration _config;

    public PrefixModule(PrefixContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [Command("add", "create")]
    [Description("Adds a command prefix.")]
    [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> AddAsync(
        [Description("The prefix to add."), NotEmpty, MaxLength(20), Inline]
        string prefix)
    {
        prefix = prefix.TrimStart();

        if (prefix.StartsWith('/'))
            return Fail("Prefix can't start with a slash character.");

        var prefixes = await _db.GetPrefixesAsync(Context.Guild);

        if (prefixes.Count >= 10)
            return Fail("You can't add more than 10 prefixes.");

        if (prefixes.Find(x => x.GuildId == Context.Guild.Id && x.Text == prefix) is not null)
            return Fail("This prefix has been already added.");

        await _db.AddPrefixAsync(Context.Guild, prefix);

        return Reply($"Prefix **{Markdown.Escape(prefix)}** has been added.").RunWith(_db.SaveChangesAsync());
    }

    [Command("remove", "delete")]
    [Description("Removes a command prefix.")]
    [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> RemoveAsync([Description("The prefix to remove."), NotEmpty] string prefix)
    {
        prefix = prefix.TrimStart();

        var dbPrefix = await _db.GetPrefixAsync(Context.Guild, prefix);

        if (dbPrefix is null)
            return Fail("Prefix hasn't been found.");

        _db.RemovePrefix(dbPrefix);

        return Reply($"Prefix **{Markdown.Escape(prefix)}** has been removed.").RunWith(_db.SaveChangesAsync());
    }

    [Command("list", "all")]
    [Description("Lists all available prefixes for this server.")]
    [Remarks("The order describes the priority of the prefixes. Prefixes at top will be checked first.")]
    [Commands.RequireBotChannelPermissions(
        Permission.AddReactions |
        Permission.SendEmbeds |
        Permission.UseExternalEmojis)]
    public async Task<DiscordCommandResult> ListAsync([Description("The page to start with")] int page = 1)
    {
        var prefixes = (await _db.GetPrefixesAsync(Context.Guild))
            .OrderByDescending(x => x.Text.Length)
            .ThenBy(x => x.Text)
            .ToArray();

        if (prefixes.Length == 0)
            return Reply("There aren't any prefixes for this server.");

        int count = prefixes.Length;
        int padding = count.ToString().Length;
        var pageDescriptions = new List<string>();

        var pageText = new StringBuilder();

        for (int i = 0; i < prefixes.Length; i++)
        {
            var prefix = prefixes[i];
            pageText.Append('`')
                .Append(i.ToString().PadLeft(padding))
                .Append(".` ")
                .AppendLine(Markdown.Escape(prefix.Text));

            if ((i + 1) % 15 != 0 && i + 1 != count)
                continue;

            pageDescriptions.Add(pageText.ToString());
            pageText.Clear();
        }

        if (page > pageDescriptions.Count || page < 1)
            return Fail("This page doesn't exist.");

        List<Page> pages = new();

        for (int i = 0; i < pageDescriptions.Count; i++)
        {
            var embed = new LocalEmbed()
                .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                .WithAuthor(Context.Guild.Name, Context.Guild.GetIconUrl())
                .WithTitle("Prefixes")
                .WithDescription(pageDescriptions[i])
                .WithFooter($"Page {i + 1}/{pageDescriptions.Count} ({"entry".ToQuantity(count)})");

            pages.Add(new Page().WithEmbeds(embed));
        }

        return Paginate(pages, startIndex: page - 1);
    }
}