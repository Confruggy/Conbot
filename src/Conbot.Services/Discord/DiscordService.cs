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
                    _logger.LogDebug($"{message.Source}: {{Message}}", message.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace($"{message.Source}: {{Message}}", message.Message);
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation($"{message.Source}: {{Message}}", message.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning($"{message.Source}: {{Message}}", message.Message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError($"{message.Source}: {{Message}}", message.Message);
                    break;
                case LogSeverity.Critical:
                    _logger.LogCritical($"{message.Source}: {{Message}}", message.Message);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}