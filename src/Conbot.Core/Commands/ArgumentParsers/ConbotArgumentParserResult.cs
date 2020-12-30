using System.Collections.Generic;
using Qmmands;

namespace Conbot.Commands
{
    public sealed class ConbotArgumentParserResult : ArgumentParserResult
    {
        public override bool IsSuccessful => Reason == null;
        public override string Reason { get; }

        private ConbotArgumentParserResult(IReadOnlyDictionary<Parameter, object> arguments) : base(arguments)
        {
        }

        private ConbotArgumentParserResult(string errorReason) : base(null)
        {
            Reason = errorReason;
        }

        public static ConbotArgumentParserResult Successful(IReadOnlyDictionary<Parameter, object> arguments)
            => new ConbotArgumentParserResult(arguments);

        public static ConbotArgumentParserResult Failed(string reason)
            => new ConbotArgumentParserResult(reason);
    }
}