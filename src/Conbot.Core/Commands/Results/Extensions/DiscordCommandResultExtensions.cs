using System.Threading.Tasks;

using Disqord.Bot;

namespace Conbot.Commands
{
    public static class DiscordCommandResultExtensions
    {
        public static ConbotWhenAllCommandResult RunWith(this DiscordCommandResult result, params Task[] tasks)
            => new(result, tasks);
    }
}