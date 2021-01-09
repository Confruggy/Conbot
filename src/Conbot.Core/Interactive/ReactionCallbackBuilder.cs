using System;
using System.Threading.Tasks;

using Discord;

namespace Conbot.Interactive
{
    public class ReactionCallbackBuilder
    {
        public IEmote Emote { get; set; }
        public Func<IReaction, Task> Callback { get; set; } = (_) => Task.CompletedTask;
        public bool ResumeAfterExecution { get; set; }

        public ReactionCallbackBuilder(IEmote emote)
        {
            Emote = emote;
        }

        public ReactionCallbackBuilder(string text)
        {
            if (Discord.Emote.TryParse(text, out var emote))
                Emote = emote;
            else
                Emote = new Emoji(text);
        }

        public ReactionCallbackBuilder WithCallback(Func<IReaction, Task> callback)
        {
            Callback = callback;
            return this;
        }

        public ReactionCallbackBuilder WithCallback(Action<IReaction> callback)
        {
            Callback = x =>
                {
                    callback(x);
                    return Task.CompletedTask;
                };
            return this;
        }

        public ReactionCallbackBuilder ShouldResumeAfterExecution(bool resumeAfterExecution)
        {
            ResumeAfterExecution = resumeAfterExecution;
            return this;
        }

        public ReactionCallback Build() => new(Emote, Callback, ResumeAfterExecution);
    }
}