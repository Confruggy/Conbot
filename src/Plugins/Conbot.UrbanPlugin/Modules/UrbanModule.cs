using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.WebUtilities;

using Conbot.Commands;
using Conbot.Interactive;

using Discord;

using Humanizer;

using Qmmands;

namespace Conbot.UrbanPlugin
{
    [Name("Urban Dictionary")]
    [Description("Explore slang words and phrases from Urban Dictionary.")]
    [Group("urban", "u")]
    [RequireBotPermission(
        ChannelPermission.AddReactions |
        ChannelPermission.EmbedLinks |
        ChannelPermission.UseExternalEmojis)]
    internal class UrbanModule : DiscordModuleBase
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
        public async Task<CommandResult> SearchAsync([Remainder, Description("The word to search for.")] string word)
        {
            var searchResult = await _service.SearchAsync(word);
            return await UrbanAsync(searchResult);
        }

        [Command("random")]
        [Description("Searches a definition for a random word.")]
        public async Task<CommandResult> RandomAsync()
        {
            var searchResult = await _service.GetRandomAsync();
            return await UrbanAsync(searchResult);
        }

        public async Task<CommandResult> UrbanAsync(UrbanSearchResult searchResult)
        {
            int count = searchResult.Results.Count();

            var result = searchResult.Results.FirstOrDefault();

            if (result == null)
                return Unsuccessful("No definition has been found for this word.");

            var paginator = new Paginator();

            for (int i = 0; i < count; i++)
                paginator.AddPage(CreateUrbanEmbed(searchResult.Results.ElementAt(i), i + 1, count));

            await paginator.RunAsync(_interactiveService, Context);
            return Successful;
        }

        private Embed CreateUrbanEmbed(UrbanResult result, int currentPage, int totalPages)
        {
            var embed = new EmbedBuilder()
                .WithColor(new Color(0x134fe6))
                .WithAuthor(result.Author, url:
                    QueryHelpers.AddQueryString($"{_service.WebsiteBaseUrl}/author.php", "author", result.Author))
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
            foreach (Match match in Regex.Matches(text, "\\[[^\\]]*\\]"))
            {
                string url = $"{_service.WebsiteBaseUrl}/define.php?term={HttpUtility.UrlEncode(match.Value[1..^1])}";
                text = text.Replace(match.Value, $"{match.Value}({url})");
            }

            return text;
        }
    }
}
