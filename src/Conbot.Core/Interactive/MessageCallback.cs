using System;
using System.Threading.Tasks;

using Disqord.Gateway;

namespace Conbot.Interactive;

public class MessageCallback
{
    public Func<IInteractiveUserMessage, MessageReceivedEventArgs, Task<bool>>? Precondition { get; }

    public Func<IInteractiveUserMessage, MessageReceivedEventArgs, Task> Callback { get; }

    public MessageCallback(Func<IInteractiveUserMessage, MessageReceivedEventArgs, Task<bool>>? precondition,
        Func<IInteractiveUserMessage, MessageReceivedEventArgs, Task> callback)
    {
        Precondition = precondition;
        Callback = callback;
    }

    public MessageCallback(LocalMessageCallback messageCallback)
        : this(messageCallback.Precondition, messageCallback.Callback)
    {
    }
}