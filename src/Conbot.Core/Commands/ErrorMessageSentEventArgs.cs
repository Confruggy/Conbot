using System;

using Discord;

using Qmmands;

namespace Conbot.Commands
{
    public class CommandErrorMessageSentEventArgs : EventArgs
    {
        public IUserMessage Message { get; set; }
        public DiscordCommandContext Context { get; set; }
        public FailedResult Result { get; set; }

        public CommandErrorMessageSentEventArgs(IUserMessage message, DiscordCommandContext context,
            FailedResult result)
        {
            Message = message;
            Context = context;
            Result = result;
        }
    }
}
