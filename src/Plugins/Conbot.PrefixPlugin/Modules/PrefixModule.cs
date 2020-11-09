using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Services.Interactive;
using Discord;
using Humanizer;
using Qmmands;

namespace Conbot.PrefixPlugin
{
    [Name("Prefix")]
    [Group("prefix")]
    [Description("Sets up command prefixes.")]
    [Remarks("You can set up up to 10 prefixes for this server. Commands can also be invoked by mentioning the bot.")]
    [RequireContext(ContextType.Guild)]
    public class PrefixModule : DiscordModuleBase
    {
        private readonly InteractiveService _interactiveService;
        private readonly PrefixContext _db;

        public PrefixModule(InteractiveService interactiveSerivce, PrefixContext db)
        {
            _interactiveService = interactiveSerivce;
            _db = db;
        }

        [Command("add", "create")]
        [Description("Adds a command prefix.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task AddAsync(
            [Description("The prefix to add."), NotEmpty, MaxLength(20)] string prefix)
        {
            prefix = prefix.TrimStart();

            var prefixes = await _db.GetPrefixesAsync(Context.Guild);

            if (prefixes.Count >= 10)
            {
                await ReplyAsync("You can't add more than 10 prefixes.");
                return;
            }

            if (prefixes.Find(x => x.GuildId == Context.Guild.Id && x.Text == prefix) != null)
            {
                await ReplyAsync("This prefix has been already added.");
                return;
            }

            await _db.AddPrefixAsync(Context.Guild, prefix);

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync($"Prefix **{Format.Sanitize(prefix)}** has been added."));
        }

        [Command("remove", "delete")]
        [Description("Removes a command prefix.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task RemoveAsync([Description("The prefix to remove."), NotEmpty] string prefix)
        {
            prefix = prefix.TrimStart();

            var dbPrefix = await _db.GetPrefixAsync(Context.Guild, prefix);

            if (dbPrefix == null)
            {
                await ReplyAsync("Prefix hasn't been found.");
                return;
            }

            _db.RemovePrefix(dbPrefix);

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync($"Prefix **{Format.Sanitize(prefix)}** has been removed."));
        }

        [Command("list", "all")]
        [Description("Lists all available prefixes for this server.")]
        [Remarks("The order describes the priority of the prefixes. Prefixes at top will be checked first.")]
        public async Task ListAsync([Description("The page to start with")] int page = 1)
        {
            var prefixes = (await _db.GetPrefixesAsync(Context.Guild))
                .OrderByDescending(x => x.Text.Length)
                .ThenBy(x => x.Text);

            if (!prefixes.Any())
            {
                await ReplyAsync("There aren't any prefixes for this server.");
                return;
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
            {
                await ReplyAsync("This page doesn't exist.");
                return;
            }

            var paginator = new Paginator();

            for (int j = 0; j < pages.Count; j++)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Constants.DefaultEmbedColor)
                    .WithAuthor( Context.Guild.Name, Context.Guild.IconUrl)
                    .WithTitle("Prefixes")
                    .WithDescription(pages[j])
                    .WithFooter($"Page {j + 1}/{pages.Count} ({"entry".ToQuantity(count)})")
                    .Build();
                paginator.AddPage(embed);
            }

            await paginator.RunAsync(_interactiveService, Context, page - 1);
        }
    }
}