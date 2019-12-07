using System;
using System.Linq;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Logging;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Conbot
{
    public class ConbotClient
    {
        private readonly Config _config;
        private readonly DiscordShardedClient _discordClient;

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
        }

        public async Task RunAsync()
        {
            await _discordClient.LoginAsync(TokenType.Bot, _config.Token);
            await _discordClient.StartAsync();

            var services = new ServiceCollection();
            ConfigureServices(services);
            
            var provider = services.BuildServiceProvider();
            await InstallServicesAsync(provider);
        }

        private void SubscribeEvents()
        {
            _discordClient.LeftGuild += OnJoinedGuild;
            _discordClient.LeftGuild += OnLeftGuild;
            _discordClient.ShardReady += OnShardReady;
            _discordClient.Log += ConsoleLog.LogAsync;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton(_discordClient)
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    CaseSensitiveCommands = false,
                    DefaultRunMode = RunMode.Sync,
                    LogLevel = LogSeverity.Debug,
                }))
                .AddSingleton<CommandHandler>()
                .AddSingleton<Random>();
        }

        private async Task InstallServicesAsync(IServiceProvider provider)
        {
            await provider.GetRequiredService<CommandHandler>().StartAsync();
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