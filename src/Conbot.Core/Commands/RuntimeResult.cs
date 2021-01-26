using Discord;

using Qmmands;

namespace Conbot.Commands
{
    public class RuntimeResult : CommandResult
    {
        public string? ErrorReason { get; }
        public IUserMessage? Message { get; set; }
        public override bool IsSuccessful => ErrorReason is null;

        public static RuntimeResult Successful => new();

        internal RuntimeResult(string? errorReason = null, IUserMessage? message = null)
        {
            ErrorReason = errorReason;
            Message = message;
        }

        public static RuntimeResult Unsuccessful(string reason) => new(reason);

        public static RuntimeResult Unsuccessful(IUserMessage message) => new(message.Content, message);
    }
}
