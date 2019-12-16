using System;
using System.Threading;
using Conbot.InteractiveMessages;
using Discord;

namespace Conbot.Services.Interactive
{
    internal class ExecutingInteractiveMessage
    {
        internal InteractiveMessage InteractiveMessage { get; set; }
        internal IUser User { get; set; }
        internal IUserMessage Message { get; set; }
        internal DateTimeOffset TimeoutDate { get; set; }
        internal CancellationTokenSource TokenSource { get; set; }
    }
}