using System;
using System.Threading.Tasks;

using Discord;

namespace Conbot.Interactive
{
    public class ReactionCallback
    {
        public IEmote Emote { get; }
        public Func<IReaction, Task> Callback { get; }
        public bool ResumeAfterExecution { get; }

        public ReactionCallback(IEmote emote, Func<IReaction, Task> callback, bool resumeAfterExecution)
        {
            Emote = emote;
            Callback = callback;
            ResumeAfterExecution = resumeAfterExecution;
        }
    }
}