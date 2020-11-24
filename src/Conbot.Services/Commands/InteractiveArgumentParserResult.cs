using System.Collections.Generic;
using Qmmands;

namespace Conbot.Commands
{
    public sealed class InteractiveArgumentParserResult : ArgumentParserResult
    {
        public override bool IsSuccessful => Reason == null;
        public override string Reason { get; }

        private InteractiveArgumentParserResult(IReadOnlyDictionary<Parameter, object> arguments) : base(arguments)
        {
        }

        private InteractiveArgumentParserResult(string errorReason) : base(null)
        {
            Reason = errorReason;
        }

        public static InteractiveArgumentParserResult Successful(IReadOnlyDictionary<Parameter, object> arguments)
            => new InteractiveArgumentParserResult(arguments);

        public static InteractiveArgumentParserResult Failed(string reason)
            => new InteractiveArgumentParserResult(reason);
    }
}