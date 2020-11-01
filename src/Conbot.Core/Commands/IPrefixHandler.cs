using System.Threading.Tasks;

namespace Conbot.Commands
{
    public interface IPrefixHandler
    {
        Task<bool> HandlePrefixAsync(DiscordCommandContext context, out string output);
    }
}