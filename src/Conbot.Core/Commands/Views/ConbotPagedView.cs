using Disqord;
using Disqord.Extensions.Interactivity.Menus.Paged;

using Microsoft.Extensions.Configuration;

namespace Conbot.Commands
{
    public class ConbotPagedView : PagedView
    {
        public ConbotPagedView(PageProvider pageProvider, LocalMessage? templateMessage = null, int startindex = 0,
            IConfiguration? config = null)
            : base(pageProvider, templateMessage)
        {
            FirstPageButton.Style = LocalButtonComponentStyle.Primary;
            PreviousPageButton.Style = LocalButtonComponentStyle.Primary;
            NextPageButton.Style = LocalButtonComponentStyle.Primary;
            LastPageButton.Style = LocalButtonComponentStyle.Primary;
            StopButton.Style = LocalButtonComponentStyle.Danger;

            if (config is not null)
            {
                FirstPageButton.Emoji = LocalEmoji.FromString(config["Emotes:First"]);
                PreviousPageButton.Emoji = LocalEmoji.FromString(config["Emotes:Backward"]);
                NextPageButton.Emoji = LocalEmoji.FromString(config["Emotes:Forward"]);
                LastPageButton.Emoji = LocalEmoji.FromString(config["Emotes:Last"]);
                StopButton.Emoji = LocalEmoji.FromString(config["Emotes:Stop"]);
            }

            CurrentPageIndex = startindex;
        }

        protected override void ApplyPageIndex(Page page) { }
    }
}
