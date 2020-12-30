using System;
using System.Threading.Tasks;
using Discord;

namespace Conbot.Interactive
{
    public class ReactionCallbackBuilder
    {
        public IEmote Emote { get; set; }
        public Func<IReaction, Task> Callback { get; set; }
        public bool ResumeAfterExecution { get; set; }

        public ReactionCallbackBuilder WithEmote(IEmote emote)
        {
            Emote = emote;
            return this;
        }

        public ReactionCallbackBuilder WithEmote(string text)
        {
            if (Discord.Emote.TryParse(text, out var emote))
            {
                Emote = emote;
                return this;
            }

            Emote = new Emoji(text);
            return this;
        }

        public ReactionCallbackBuilder WithCallback(Func<IReaction, Task> callback)
        {
            Callback = callback;
            return this;
        }

        public ReactionCallbackBuilder WithCallback(Func<IReaction, bool> callback)
        {
            Callback = x => Task.FromResult(callback(x));
            return this;
        }

        public ReactionCallbackBuilder ShouldResumeAfterExecution(bool resumeAfterExecution)
        {
            ResumeAfterExecution = resumeAfterExecution;
            return this;
        }

        public ReactionCallback Build() => new ReactionCallback(Emote, Callback, ResumeAfterExecution);
    }
}