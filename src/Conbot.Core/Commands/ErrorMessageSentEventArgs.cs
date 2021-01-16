using System;

using Discord.Rest;

using Qmmands;

namespace Conbot.Commands
{
    public class CommandErrorMessageSentEventArgs : EventArgs
    {
        public RestUserMessage Message { get; set; }
        public DiscordCommandContext Context { get; set; }
        public FailedResult Result { get; set; }

        public CommandErrorMessageSentEventArgs(RestUserMessage message, DiscordCommandContext context,
            FailedResult result)
        {
            Message = message;
            Context = context;
            Result = result;
        }
    }
}
