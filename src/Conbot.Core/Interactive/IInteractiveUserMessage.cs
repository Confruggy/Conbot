using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Disqord;

namespace Conbot.Interactive
{
    public interface IInteractiveUserMessage : IUserMessage
    {
        Func<IUser, Task<bool>>? Precondition { get; }
        int Timeout { get; }
        IReadOnlyDictionary<LocalEmoji, ReactionCallback> ReactionCallbacks { get; }
        IReadOnlyCollection<MessageCallback> MessageCallbacks { get; }
        IUser User { get; }
        DateTimeOffset TimeoutsAt { get; }
        CancellationTokenSource TokenSource { get; }

        void Stop();
    }
}