using System;
using System.Threading.Tasks;
using Discord;

namespace Conbot.InteractiveMessages
{
    public class MessageCallbackBuilder
    {
        public Func<IUserMessage, Task<bool>> Precondition { get; set; }
        public Func<IUserMessage, Task> Callback { get; set; }
        public bool ResumeAfterExecution { get; set; }

        public MessageCallbackBuilder WithPrecondition(Func<IUserMessage, Task<bool>> preconditon)
        {
            Precondition = preconditon;
            return this;
        }

        public MessageCallbackBuilder WithPrecondition(Func<IUserMessage, bool> preconditon)
        {
            Precondition = x => Task.FromResult(preconditon(x));
            return this;
        }

        public MessageCallbackBuilder WithCallback(Func<IUserMessage, Task> callback)
        {
            Callback = callback;
            return this;
        }

        public MessageCallbackBuilder WithCallback(Action<IUserMessage> callback)
        {
            Callback = x => { callback(x); return Task.CompletedTask; };
            return this;
        }

        public MessageCallbackBuilder ShouldResumeAfterExecution(bool resumeAfterExecution)
        {
            ResumeAfterExecution = resumeAfterExecution;
            return this;
        }

        public MessageCallback Build() => new MessageCallback(Precondition, Callback, ResumeAfterExecution);
    }
}