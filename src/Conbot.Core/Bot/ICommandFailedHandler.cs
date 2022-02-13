using System.Threading.Tasks;

using Conbot.Commands;

using Qmmands;

namespace Conbot;

public interface ICommandFailedHandler
{
    ValueTask HandleFailedResultAsync(ConbotCommandContext context, FailedResult result);
}