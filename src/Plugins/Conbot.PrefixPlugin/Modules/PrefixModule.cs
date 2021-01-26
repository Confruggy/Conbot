using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Interactive;

using Discord;

using Humanizer;

using Qmmands;

namespace Conbot.PrefixPlugin
{
    [Name("Prefix")]
    [Group("prefix")]
    [Description("Sets up command prefixes for text commands.")]
    [Remarks(
        "You can set up up to 10 prefixes for this server. " +
        "Commands can also be invoked by mentioning the bot or using Slash Commands. " +
        "Custom prefixes are only supported by text commands.")]
    [RequireContext(ContextType.Guild)]
    public class PrefixModule : DiscordModuleBase
    {
        private readonly InteractiveService _interactiveService;
        private readonly PrefixContext _db;
        private readonly IConfiguration _config;

        public PrefixModule(InteractiveService interactiveService, PrefixContext db, IConfiguration config)
        {
            _interactiveService = interactiveService;
            _db = db;
            _config = config;
        }

        [Command("add", "create")]
        [Description("Adds a command prefix.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task<CommandResult> AddAsync(
            [Description("The prefix to add."), NotEmpty, MaxLength(20), Inline] string prefix)
        {
            prefix = prefix.TrimStart();

            if (prefix.StartsWith('/'))
                return Unsuccessful("Prefix can't start with a slash character.");

            var prefixes = await _db.GetPrefixesAsync(Context.Guild!);

            if (prefixes.Count >= 10)
                return Unsuccessful("You can't add more than 10 prefixes.");

            if (prefixes.Find(x => x.GuildId == Context.Guild.Id && x.Text == prefix) != null)
                return Unsuccessful("This prefix has been already added.");

            await _db.AddPrefixAsync(Context.Guild, prefix);

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync($"Prefix **{Format.Sanitize(prefix)}** has been added.")
            );

            return Successful;
        }

        [Command("remove", "delete")]
        [Description("Removes a command prefix.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task<CommandResult> RemoveAsync([Description("The prefix to remove."), NotEmpty] string prefix)
        {
            prefix = prefix.TrimStart();

            var dbPrefix = await _db.GetPrefixAsync(Context.Guild!, prefix);

            if (dbPrefix == null)
                return Unsuccessful("Prefix hasn't been found.");

            _db.RemovePrefix(dbPrefix);

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync($"Prefix **{Format.Sanitize(prefix)}** has been removed.")
            );

            return Successful;
        }

        [Command("list", "all")]
        [Description("Lists all available prefixes for this server.")]
        [Remarks("The order describes the priority of the prefixes. Prefixes at top will be checked first.")]
        [RequireBotPermission(
            ChannelPermission.AddReactions |
            ChannelPermission.EmbedLinks |
            ChannelPermission.UseExternalEmojis)]
        public async Task<CommandResult> ListAsync([Description("The page to start with")] int page = 1)
        {
            var prefixes = (await _db.GetPrefixesAsync(Context.Guild!))
                .OrderByDescending(x => x.Text.Length)
                .ThenBy(x => x.Text);

            if (!prefixes.Any())
            {
                await ReplyAsync("There aren't any prefixes for this server.");
                return Successful;
            }

            int count = prefixes.Count();
            int padding = count.ToString().Length;
            var pages = new List<string>();

            int i = 1;
            var pageText = new StringBuilder();

            foreach (var prefix in prefixes)
            {
                pageText.Append('`')
                    .Append(i.ToString().PadLeft(padding))
                    .Append(".` ")
                    .AppendLine(Format.Sanitize(prefix.Text));

                if (i % 15 == 0 || i == count)
                {
                    pages.Add(pageText.ToString());
                    pageText.Clear();
                }

                i++;
            }

            if (page > pages.Count || page < 1)
                return Unsuccessful("This page doesn't exist.");

            var paginator = new Paginator();

            for (int j = 0; j < pages.Count; j++)
            {
                var embed = new EmbedBuilder()
                    .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                    .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                    .WithTitle("Prefixes")
                    .WithDescription(pages[j])
                    .WithFooter($"Page {j + 1}/{pages.Count} ({"entry".ToQuantity(count)})")
                    .Build();
                paginator.AddPage(embed);
            }

            await paginator.RunAsync(_interactiveService, Context, page - 1);
            return Successful;
        }
    }
}
