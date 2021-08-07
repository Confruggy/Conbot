using System;
using System.Threading.Tasks;

using Disqord;
using Disqord.Gateway;

namespace Conbot.Interactive
{
    public class ReactionCallback
    {
        public LocalEmoji Emoji { get; }

        public Func<IInteractiveUserMessage, ReactionAddedEventArgs, Task> Callback { get; }
            = (_, _) => Task.CompletedTask;

        public bool AutoReact { get; }

        public ReactionCallback(LocalEmoji emoji, Func<IInteractiveUserMessage, ReactionAddedEventArgs, Task> callback,
            bool autoReact)
        {
            Emoji = emoji;
            Callback = callback;
            AutoReact = autoReact;
        }

        public ReactionCallback(LocalReactionCallback reactionCallback)
            : this(reactionCallback.Emoji, reactionCallback.Callback, reactionCallback.AutoReact)
        { }
    }
}
