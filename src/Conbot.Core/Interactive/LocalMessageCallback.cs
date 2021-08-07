using System;
using System.Threading.Tasks;

using Disqord.Gateway;

namespace Conbot.Interactive
{
    public class LocalMessageCallback
    {
        public Func<IInteractiveUserMessage, MessageReceivedEventArgs, Task<bool>>? Precondition { get; set; }

        public Func<IInteractiveUserMessage, MessageReceivedEventArgs, Task> Callback { get; set; }
            = (_, _) => Task.CompletedTask;
    }
}