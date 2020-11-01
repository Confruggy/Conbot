using System.Threading.Tasks;

namespace Conbot.Commands
{
    public interface IPrefixHandler
    {
        ValueTask<bool> HandlePrefixAsync(DiscordCommandContext context, out string output);
    }
}