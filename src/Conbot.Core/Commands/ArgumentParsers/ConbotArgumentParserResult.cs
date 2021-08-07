using System.Collections.Generic;

using Qmmands;

namespace Conbot.Commands
{
    public sealed class ConbotArgumentParserResult : ArgumentParserResult
    {
        public override bool IsSuccessful => FailureReason is null;
        public override string? FailureReason { get; }

        public ConbotArgumentParserResult(IReadOnlyDictionary<Parameter, object?> arguments)
            : base(arguments) { }

        public ConbotArgumentParserResult(string failureReason)
            : base(null)
        {
            FailureReason = failureReason;
        }

        public static ConbotArgumentParserResult Successful(IReadOnlyDictionary<Parameter, object?> arguments)
            => new(arguments);

        public static ConbotArgumentParserResult Failed(string reason)
            => new(reason);
    }
}