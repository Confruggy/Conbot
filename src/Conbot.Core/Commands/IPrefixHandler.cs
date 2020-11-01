using System.Threading.Tasks;

namespace Conbot.Commands
{
    public interface IPrefixHandler
    {
        ValueTask<PrefixResult> HandlePrefixAsync(DiscordCommandContext context);
    }
}