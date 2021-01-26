using Discord;

using Qmmands;

namespace Conbot.Commands
{
    public class RuntimeFailedResult : FailedResult
    {
        public Command Command { get; }
        public IUserMessage? Message { get; }
        public override string Reason { get; }

        internal RuntimeFailedResult(Command command, string errorReason, IUserMessage? message)
        {
            Command = command;
            Reason = errorReason;
            Message = message;
        }

        public RuntimeFailedResult(Command command, string errorReason)
            : this(command, errorReason, null) { }

        public RuntimeFailedResult(Command command, IUserMessage message)
            : this(command, message.Content, message) { }
    }
}
