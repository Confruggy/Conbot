using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Commands;

using Qmmands;

namespace Conbot.ReminderPlugin
{
    public class ReminderPluginService : IHostedService
    {
        private readonly IServiceProvider _provider;
        private readonly CommandService _commandService;
        private readonly SlashCommandService _slashCommandService;

        public ReminderPluginService(IServiceProvider provider, CommandService commandService,
            SlashCommandService slashCommandService)
        {
            _provider = provider;
            _commandService = commandService;
            _slashCommandService = slashCommandService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateDatabaseAsync();
            _commandService.AddArgumentParser(new ReminderArgumentParser());
            await _slashCommandService.RegisterModuleAsync<ReminderModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveArgumentParser<ReminderArgumentParser>();
            return Task.CompletedTask;
        }

        private async Task UpdateDatabaseAsync()
        {
            using var serviceScope = _provider
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();

            using var context = serviceScope.ServiceProvider.GetRequiredService<ReminderContext>();
            await context.Database.MigrateAsync();
        }
    }
}
