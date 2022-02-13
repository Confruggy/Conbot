using System;
using System.Threading.Tasks;

using Disqord.Gateway;

namespace Conbot.Interactive;

public static class LocalReactionCallbackExtensions
{
    public static LocalReactionCallback WithCallback(this LocalReactionCallback localReactionCallback,
        Func<IInteractiveUserMessage, ReactionAddedEventArgs, Task> callback)
    {
        localReactionCallback.Callback = callback;
        return localReactionCallback;
    }

    public static LocalReactionCallback WithCallback(this LocalReactionCallback localReactionCallback,
        Action<IInteractiveUserMessage, ReactionAddedEventArgs> callback)
    {
        localReactionCallback.Callback = (m, e) =>
        {
            callback(m, e);
            return Task.CompletedTask;
        };

        return localReactionCallback;
    }

    public static LocalReactionCallback WithAutoReact(this LocalReactionCallback localReactionCallback,
        bool autoReact)
    {
        localReactionCallback.AutoReact = autoReact;
        return localReactionCallback;
    }
}