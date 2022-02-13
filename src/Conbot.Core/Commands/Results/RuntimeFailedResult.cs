using Qmmands;

namespace Conbot.Commands;

public class RuntimeFailedResult : FailedResult
{
    public Command Command { get; }
    public override string FailureReason { get; }

    internal RuntimeFailedResult(Command command, string errorReason)
    {
        Command = command;
        FailureReason = errorReason;
    }
}
