using System.Linq;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Logging;
using Discord;
using Discord.WebSocket;
using Humanizer;

namespace Conbot
{
    public class ConbotClient
    {
        private readonly Config _config;
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandHandler _commandHandler;

        public ConbotClient(Config config)
        {
            _config = config;

            ConsoleLog.Severity = _config.LogSeverity;

            _discordClient = new DiscordShardedClient(new DiscordSocketConfig
            {
                TotalShards = _config.TotalShards,
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 100,
                DefaultRetryMode = RetryMode.AlwaysRetry
            });

            SubscribeEvents();

            _commandHandler = new CommandHandler(_discordClient);
        }

        public async Task RunAsync()
        {
            await _discordClient.LoginAsync(TokenType.Bot, _config.Token);
            await _discordClient.StartAsync();
            await _commandHandler.InstallAsync();
        }

        private void SubscribeEvents()
        {
            _discordClient.LeftGuild += OnJoinedGuild;
            _discordClient.LeftGuild += OnLeftGuild;
            _discordClient.ShardReady += OnShardReady;
            _discordClient.Log += ConsoleLog.LogAsync;
        }

        private async Task OnShardReady(DiscordSocketClient client)
        {
            foreach (var shard in _discordClient.Shards)
                await UpdateGameAsync(shard);
        }

        private Task OnJoinedGuild(SocketGuild guild)
            => UpdateGameAsync(_discordClient.GetShardFor(guild));

        private Task OnLeftGuild(SocketGuild guild)
            => UpdateGameAsync(_discordClient.GetShardFor(guild));

        private Task UpdateGameAsync(DiscordSocketClient client)
            => _discordClient.SetGameAsync($"{"server".ToQuantity(client.Guilds.Count())} | conbot.moe");
    }
}