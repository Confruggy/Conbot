using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Conbot.Services;
using Conbot.Services.Urban;
using Discord;
using Discord.Commands;
using Humanizer;
using Microsoft.AspNetCore.WebUtilities;
using System.Web;
using Conbot.Services.Interactive;

namespace Conbot.Modules.Urban
{
    [Name("Urban Dictionary")]
    [Summary("Explore slang words and phrases from Urban Dictionary.")]
    [Group("urban"), Alias("u")]
    [RequireBotPermission(ChannelPermission.EmbedLinks)]
    [RequireBotPermission(ChannelPermission.AddReactions)]
    public class UrbanModule : ModuleBase<SocketCommandContext>
    {
        private readonly UrbanService _service;
        private readonly InteractiveService _interactiveService;

        public UrbanModule(UrbanService service, InteractiveService interactiveService)
        {
            _service = service;
            _interactiveService = interactiveService;
        }

        [Command]
        [Summary("Searches a definition for a word.")]
        [Priority(-1)]
        public async Task UrbanAsync([Remainder, Summary("The word to search for.")] string word)
        {
            var searchResult = await _service.SearchAsync(word);
            await UrbanAsync(searchResult);
        }

        [Command("random")]
        [Summary("Searches a definition for a random word.")]
        public async Task RandomAsync()
        {
            var searchResult = await _service.GetRandomAsync();
            await UrbanAsync(searchResult);
        }

        public async Task UrbanAsync(UrbanSearchResult searchResult)
        {
            int count = searchResult.Results.Count();

            var result = searchResult.Results.FirstOrDefault();

            if (result == null)
            {
                await ReplyAsync("No definition has been found for this word.");
                return;
            }

            var paginator = new Paginator();

            for (int i = 0; i < count; i++)
                paginator.AddPage(CreateUrbanEmbed(searchResult.Results.ElementAt(i), i + 1, count));

            await paginator.RunAsync(_interactiveService, Context);
        }

        private Embed CreateUrbanEmbed(UrbanResult result, int currentPage, int totalPages)
        {
            var embed = new EmbedBuilder()
                .WithColor(new Color(0x134fe6))
                .WithAuthor(result.Author, url:
                    QueryHelpers.AddQueryString("http://www.urbandictionary.com/author.php", "author", result.Author))
                .WithTitle(result.Word)
                .WithUrl(result.Permalink)
                .WithDescription(Format.Sanitize(FillHyperlinks(result.Definition)).Truncate(2048))
                .WithFooter($"Definition {currentPage}/{totalPages}");

            if (!string.IsNullOrEmpty(result.Example))
                embed.AddField("Example", $"_{Format.Sanitize(FillHyperlinks(result.Example)).Truncate(1022)}_");

            embed.AddField("Rating", $"👍 **{result.ThumbsUp}** | 👎 **{result.ThumbsDown}**");

            return embed.Build();
        }

        private string FillHyperlinks(string text)
        {
            string baseUrl = "https://www.urbandictionary.com/define.php?term=";

            foreach (Match match in Regex.Matches(text, "\\[[^\\]]*\\]"))
            {
                string url = $"{baseUrl}{HttpUtility.UrlEncode(match.Value[1..^1])}";
                text = text
                    .Replace(match.Value, $"{match.Value}({url})");
            }

            return text;
        }
    }
}