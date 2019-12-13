using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conbot.Logging;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.Hosting;

namespace Conbot.Services.Discord
{
    public class DiscordService : IHostedService
    {
        private readonly DiscordShardedClient _client;
        private readonly Config _config;

        public DiscordService(DiscordShardedClient client, Config config)
        {
            _client = client;
            _config = config;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _client.LeftGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeftGuild;
            _client.ShardReady += OnShardReady;
            _client.Log += ConsoleLog.LogAsync;

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _client.LeftGuild -= OnJoinedGuild;
            _client.LeftGuild -= OnLeftGuild;
            _client.ShardReady -= OnShardReady;
            _client.Log -= ConsoleLog.LogAsync;

            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private async Task OnShardReady(DiscordSocketClient client)
        {
            foreach (var shard in _client.Shards)
                await UpdateGameAsync(shard);
        }

        private Task OnJoinedGuild(SocketGuild guild)
            => UpdateGameAsync(_client.GetShardFor(guild));

        private Task OnLeftGuild(SocketGuild guild)
            => UpdateGameAsync(_client.GetShardFor(guild));

        private Task UpdateGameAsync(DiscordSocketClient client)
            => _client.SetGameAsync($"{"server".ToQuantity(client.Guilds.Count())} | conbot.moe");
    }
}