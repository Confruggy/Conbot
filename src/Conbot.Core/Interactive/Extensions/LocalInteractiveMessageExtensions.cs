using System;
using System.Threading.Tasks;

using Disqord;

namespace Conbot.Interactive;

public static class LocalInteractiveMessageExtensions
{
    public static LocalInteractiveMessage WithPrecondition(this LocalInteractiveMessage localInteractiveMessage,
        Func<IUser, Task<bool>> precondition)
    {
        localInteractiveMessage.Precondition = precondition;
        return localInteractiveMessage;
    }

    public static LocalInteractiveMessage WithPrecondition(this LocalInteractiveMessage localInteractiveMessage,
        Func<IUser, bool> precondition)
    {
        localInteractiveMessage.Precondition = x => Task.FromResult(precondition(x));
        return localInteractiveMessage;
    }

    public static LocalInteractiveMessage WithTimeout(this LocalInteractiveMessage localInteractiveMessage, int ms)
    {
        localInteractiveMessage.Timeout = ms;
        return localInteractiveMessage;
    }

    public static LocalInteractiveMessage AddReactionCallback(this LocalInteractiveMessage localInteractiveMessage,
        LocalReactionCallback reactionCallback)
    {
        localInteractiveMessage.ReactionCallbacks.Add(reactionCallback.Emoji, reactionCallback);
        return localInteractiveMessage;
    }

    public static LocalInteractiveMessage AddReactionCallback(this LocalInteractiveMessage localInteractiveMessage,
        LocalEmoji emoji, Func<LocalReactionCallback, LocalReactionCallback> reactionCallbackFunc)
    {
        var reactionCallback = reactionCallbackFunc(new LocalReactionCallback(emoji));
        localInteractiveMessage.ReactionCallbacks.Add(reactionCallback.Emoji, reactionCallback);
        return localInteractiveMessage;
    }

    public static LocalInteractiveMessage AddReactionCallback(this LocalInteractiveMessage localInteractiveMessage,
        string emoji,
        Func<LocalReactionCallback, LocalReactionCallback> reactionCallbackFunc)
    {
        var reactionCallback = reactionCallbackFunc(new LocalReactionCallback(emoji));
        localInteractiveMessage.ReactionCallbacks.Add(reactionCallback.Emoji, reactionCallback);
        return localInteractiveMessage;
    }

    public static LocalInteractiveMessage AddMessageCallback(this LocalInteractiveMessage localInteractiveMessage,
        LocalMessageCallback messageCallback)
    {
        localInteractiveMessage.MessageCallbacks.Add(messageCallback);
        return localInteractiveMessage;
    }

    public static LocalInteractiveMessage AddMessageCallback(this LocalInteractiveMessage localInteractiveMessage,
        Func<LocalMessageCallback, LocalMessageCallback> messageCallbackFunc)
    {
        var messageCallback = messageCallbackFunc(new LocalMessageCallback());
        localInteractiveMessage.MessageCallbacks.Add(messageCallback);
        return localInteractiveMessage;
    }
}