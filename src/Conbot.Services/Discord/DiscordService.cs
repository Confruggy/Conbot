using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conbot.Services.Discord
{
    public class DiscordService : IHostedService
    {
        private readonly DiscordShardedClient _client;
        private readonly IConfiguration _config;
        private readonly ILogger<DiscordService> _logger;

        public DiscordService(DiscordShardedClient client, IConfiguration config, ILogger<DiscordService> logger)
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

            await _client.LoginAsync(TokenType.Bot, _config["Discord:Token"]);
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
            => _client.SetGameAsync($"{"server".ToQuantity(client.Guilds.Count)} | conbot.moe");

        private Task OnLogAsync(LogMessage message)
        {
            _logger.Log(LogLevelFromSeverity(message.Severity), message.Exception,
                $"{message.Source}: {{Message}}", message.Message);
            return Task.CompletedTask;
        }

        private static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)Math.Abs((int)severity - 5);
    }
}