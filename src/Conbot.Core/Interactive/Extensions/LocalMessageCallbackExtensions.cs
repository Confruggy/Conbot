using System;
using System.Threading.Tasks;

using Disqord.Gateway;

namespace Conbot.Interactive;

public static class LocalMessageCallbackExtensions
{
    public static LocalMessageCallback WithPrecondition(this LocalMessageCallback localMessageCallback,
        Func<IInteractiveUserMessage, MessageReceivedEventArgs, Task<bool>> preconditon)
    {
        localMessageCallback.Precondition = preconditon;
        return localMessageCallback;
    }

    public static LocalMessageCallback WithPrecondition(this LocalMessageCallback localMessageCallback,
        Func<IInteractiveUserMessage, MessageReceivedEventArgs, bool> preconditon)
    {
        localMessageCallback.Precondition = (m, e) => Task.FromResult(preconditon(m, e));
        return localMessageCallback;
    }

    public static LocalMessageCallback WithCallback(this LocalMessageCallback localMessageCallback,
        Func<IInteractiveUserMessage, MessageReceivedEventArgs, Task> callback)
    {
        localMessageCallback.Callback = callback;
        return localMessageCallback;
    }

    public static LocalMessageCallback WithCallback(this LocalMessageCallback localMessageCallback,
        Action<IInteractiveUserMessage, MessageReceivedEventArgs> callback)
    {
        localMessageCallback.Callback = (m, e) =>
        {
            callback(m, e);
            return Task.CompletedTask;
        };

        return localMessageCallback;
    }
}