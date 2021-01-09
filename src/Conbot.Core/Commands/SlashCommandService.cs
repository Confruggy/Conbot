using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using Qmmands;

namespace Conbot.Commands
{
    public class SlashCommandService
    {
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandService _commandService;
        private readonly ConcurrentDictionary<Module, List<RestGlobalCommand>> _commands;

        public SlashCommandService(DiscordShardedClient discordClient, CommandService commandService)
        {
            _discordClient = discordClient;
            _commandService = commandService;
            _commands = new ConcurrentDictionary<Module, List<RestGlobalCommand>>();
        }

        public async ValueTask<IReadOnlyCollection<RestGlobalCommand>> RegisterModuleAsync(Module module)
        {
            if (_commands.ContainsKey(module))
                throw new InvalidOperationException($"Module '{module.Name}' has been already registered.");

            var commands = new List<RestGlobalCommand>();

            string? alias = module.Aliases.FirstOrDefault();

            if (!string.IsNullOrEmpty(alias))
            {
                var slashCommand = new SlashCommandCreationProperties
                {
                    Name = alias,
                    Description = module.Description ?? "No Description."
                };

                var options = new List<ApplicationCommandOptionProperties>();

                foreach (var command in module.Commands)
                {
                    var subcommand = new ApplicationCommandOptionProperties
                    {
                        Name = command.Aliases[0],
                        Description = command.Description ?? "No Description.",
                        Type = ApplicationCommandOptionType.SubCommand,
                        Options = SlashCommandHelper.GetOptionsForCommand(command)
                    };

                    options.Add(subcommand);
                }

                foreach (var submodule in module.Submodules)
                {
                    var subcommandGroup = new ApplicationCommandOptionProperties
                    {
                        Name = submodule.Aliases[0],
                        Description = submodule.Description ?? "No Description.",
                        Type = ApplicationCommandOptionType.SubCommandGroup,
                    };

                    var subcommands = new List<ApplicationCommandOptionProperties>();

                    foreach (var command in submodule.Commands)
                    {
                        var subcommand = new ApplicationCommandOptionProperties
                        {
                            Name = command.Aliases[0],
                            Description = command.Description ?? "No Description.",
                            Type = ApplicationCommandOptionType.SubCommand,
                            Options = SlashCommandHelper.GetOptionsForCommand(command)
                        };

                        subcommands.Add(subcommand);
                    }

                    subcommandGroup.Options = subcommands;

                    options.Add(subcommandGroup);
                }

                slashCommand.Options = options;

                commands.Add(await _discordClient.Rest.CreateGlobalCommand(slashCommand));
            }
            else
            {
                foreach (var command in module.Commands)
                {
                    var slashCommand = new SlashCommandCreationProperties
                    {
                        Name = command.Aliases[0],
                        Description = command.Description ?? "No Description.",
                        Options = SlashCommandHelper.GetOptionsForCommand(command)
                    };

                    commands.Add(await _discordClient.Rest.CreateGlobalCommand(slashCommand));
                }
            }

            _commands.TryAdd(module, commands);
            return commands.AsReadOnly();
        }

        public async ValueTask<(Module, IReadOnlyCollection<RestGlobalCommand>)> RegisterModuleAsync<T>()
            where T : class
        {
            var module = _commandService.AddModule<T>();
            var commands = await RegisterModuleAsync(module);
            return (module, commands);
        }

        public async Task UnregisterModuleAsync(Module module)
        {
            if (!_commands.TryGetValue(module, out var commands))
                throw new InvalidOperationException($"Module '{module.Name}' is not registered.");

            foreach (var command in commands)
                await command.DeleteAsync();

            _commandService.RemoveModule(module);
        }
    }
}
