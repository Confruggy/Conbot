using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;

namespace Conbot.Interactive
{
    public class InteractiveMessage
    {
        public Func<IUser, Task<bool>>? Precondition { get; }
        public int Timeout { get; }
        public Dictionary<string, ReactionCallback> ReactionCallbacks { get; }
        public List<MessageCallback> MessageCallbacks { get; }
        public bool AutoReactEmotes { get; }

        public InteractiveMessage(Func<IUser, Task<bool>>? precondition, int timeout,
            Dictionary<string, ReactionCallback> reactionCallbacks,
            List<MessageCallback> messageCallbacks, bool autoReactEmotes)
        {
            Precondition = precondition;
            Timeout = timeout;
            ReactionCallbacks = reactionCallbacks;
            MessageCallbacks = messageCallbacks;
            AutoReactEmotes = autoReactEmotes;
        }
    }
}
