using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.WebUtilities;

using Conbot.Commands;
using Conbot.Interactive;

using Disqord;
using Disqord.Bot;

using Humanizer;

using Qmmands;

namespace Conbot.UrbanPlugin
{
    [Name("Urban Dictionary")]
    [Description("Explore slang words and phrases from Urban Dictionary.")]
    [Group("urban", "u")]
    [Commands.RequireBotChannelPermissions(
        Permission.AddReactions |
        Permission.SendEmbeds |
        Permission.UseExternalEmojis)]
    internal class UrbanModule : ConbotModuleBase
    {
        private readonly UrbanService _service;
        private readonly InteractiveService _interactiveService;

        public UrbanModule(UrbanService service, InteractiveService interactiveService)
        {
            _service = service;
            _interactiveService = interactiveService;
        }

        [Command("search", "")]
        [Description("Searches a definition for a word.")]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task<DiscordCommandResult> SearchAsync(
            [Remainder, Description("The word to search for.")] string word)
        {
            var searchResult = await _service.SearchAsync(word);
            return Urban(searchResult);
        }

        [Command("random")]
        [Description("Searches a definition for a random word.")]
        public async Task<DiscordCommandResult> RandomAsync()
        {
            var searchResult = await _service.GetRandomAsync();
            return Urban(searchResult);
        }

        public DiscordCommandResult Urban(UrbanSearchResult searchResult)
        {
            int count = searchResult.Results.Count();

            var result = searchResult.Results.FirstOrDefault();

            if (result is null)
                return Fail("No definition has been found for this word.");

            var paginator = new Paginator();

            for (int i = 0; i < count; i++)
                paginator.AddPage(CreateUrbanEmbed(searchResult.Results.ElementAt(i), i + 1, count));

            return Paginate(paginator);
        }

        private LocalEmbed CreateUrbanEmbed(UrbanResult result, int currentPage, int totalPages)
        {
            var embedBuilder = new LocalEmbed()
                .WithColor(new Color(0x134fe6))
                .WithAuthor(result.Author, url:
                    QueryHelpers.AddQueryString($"{_service.WebsiteBaseUrl}/author.php", "author", result.Author))
                .WithTitle(result.Word)
                .WithUrl(result.Permalink)
                .WithDescription(Markdown.Escape(FillHyperlinks(result.Definition)).Truncate(2048))
                .WithFooter($"Definition {currentPage}/{totalPages}");

            if (!string.IsNullOrEmpty(result.Example))
                embedBuilder.AddField("Example", $"_{Markdown.Escape(FillHyperlinks(result.Example)).Truncate(1022)}_");

            embedBuilder.AddField("Rating", $"👍 **{result.ThumbsUp}** | 👎 **{result.ThumbsDown}**");

            return embedBuilder;
        }

        private string FillHyperlinks(string text)
        {
            foreach (Match match in Regex.Matches(text, "\\[[^\\]]*\\]"))
            {
                string url = $"{_service.WebsiteBaseUrl}/define.php?term={HttpUtility.UrlEncode(match.Value[1..^1])}";
                text = text.Replace(match.Value, $"{match.Value}({url})");
            }

            return text;
        }
    }
}
