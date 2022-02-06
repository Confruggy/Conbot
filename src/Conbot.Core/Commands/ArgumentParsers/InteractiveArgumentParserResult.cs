using System.Collections.Generic;

using Qmmands;

namespace Conbot.Commands
{
    public sealed class InteractiveArgumentParserResult : ArgumentParserResult
    {
        public override bool IsSuccessful => FailureReason is null;
        public override string? FailureReason { get; }

        public InteractiveArgumentParserResult(IReadOnlyDictionary<Parameter, object?> arguments)
            : base(arguments) { }

        public InteractiveArgumentParserResult(string failureReason)
            : base(null)
        {
            FailureReason = failureReason;
        }

        public static InteractiveArgumentParserResult Successful(IReadOnlyDictionary<Parameter, object?> arguments)
            => new(arguments);

        public static InteractiveArgumentParserResult Failed(string reason)
            => new(reason);
    }
}
