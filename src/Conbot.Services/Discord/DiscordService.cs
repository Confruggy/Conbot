using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conbot.Services.Discord
{
    public class DiscordService : IHostedService
    {
        private readonly DiscordShardedClient _client;
        private readonly Config _config;
        private readonly ILogger<DiscordService> _logger;

        public DiscordService(DiscordShardedClient client, Config config, ILogger<DiscordService> logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _client.LeftGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeftGuild;
            _client.ShardReady += OnShardReady;
            _client.Log += OnLogAsync;

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _client.LeftGuild -= OnJoinedGuild;
            _client.LeftGuild -= OnLeftGuild;
            _client.ShardReady -= OnShardReady;
            _client.Log -= OnLogAsync;

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

        private Task OnLogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Debug:
                    _logger.LogDebug($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Error:
                    _logger.LogError($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Critical:
                    _logger.LogCritical($"{{Source}}: {message.Message}", message.Source);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}