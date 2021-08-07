using System;
using System.Threading.Tasks;

using Disqord;
using Disqord.Gateway;

namespace Conbot.Interactive
{
    public class LocalReactionCallback
    {
        public LocalEmoji Emoji { get; set; }

        public Func<IInteractiveUserMessage, ReactionAddedEventArgs, Task> Callback { get; set; }
            = (_, _) => Task.CompletedTask;

        public bool AutoReact { get; set; } = true;

        public LocalReactionCallback(LocalEmoji emoji) => Emoji = emoji;

        public LocalReactionCallback(string emoji) => Emoji = LocalEmoji.FromString(emoji);
    }
}