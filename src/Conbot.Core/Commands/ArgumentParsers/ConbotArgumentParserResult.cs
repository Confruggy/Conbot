using System.Collections.Generic;

using Qmmands;

namespace Conbot.Commands
{
    public sealed class ConbotArgumentParserResult : ArgumentParserResult
    {
        public override bool IsSuccessful => Reason == null;
        public override string? Reason { get; }

        public ConbotArgumentParserResult(IReadOnlyDictionary<Parameter, object?> arguments)
            : base(arguments) { }

        public ConbotArgumentParserResult(string errorReason)
            : base(null)
        {
            Reason = errorReason;
        }

        public static ConbotArgumentParserResult Successful(IReadOnlyDictionary<Parameter, object?> arguments)
            => new(arguments);

        public static ConbotArgumentParserResult Failed(string reason)
            => new(reason);
    }
}