using System;
using System.Threading.Tasks;

using Discord;

namespace Conbot.Interactive
{
    public class MessageCallback
    {
        public Func<IUserMessage, Task<bool>>? Precondition { get; }
        public Func<IUserMessage, Task> Callback { get; }
        public bool ResumeAfterExecution { get; }

        public MessageCallback(Func<IUserMessage, Task<bool>>? precondition, Func<IUserMessage, Task> callback,
            bool resumeAfterExecution)
        {
            Precondition = precondition;
            Callback = callback;
            ResumeAfterExecution = resumeAfterExecution;
        }
    }
}