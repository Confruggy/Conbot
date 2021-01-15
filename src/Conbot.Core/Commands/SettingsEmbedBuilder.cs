using System;
using System.Linq;
using System.Text;

using Discord;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Qmmands;

namespace Conbot.Commands
{
    public class SettingsEmbedBuilder
    {
        private readonly CommandContext _context;
        private readonly IConfiguration _config;
        private readonly CommandService _commandService;
        private readonly EmbedBuilder _embed;

        public SettingsEmbedBuilder(CommandContext context)
        {
            _context = context;

            var serviceProdier = _context.ServiceProvider;
            _config = serviceProdier.GetRequiredService<IConfiguration>();
            _commandService = serviceProdier.GetRequiredService<CommandService>();

            _embed = new EmbedBuilder()
                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"));
        }

        public SettingsEmbedBuilder WithTitle(string title)
        {
            _embed.WithTitle(title);
            return this;
        }

        public SettingsEmbedBuilder WithColor(uint color)
        {
            _embed.WithColor(color);
            return this;
        }

        public SettingsEmbedBuilder WithUser(IUser user)
        {
            _embed.WithFooter(user.ToString(), user.GetAvatarUrl());
            return this;
        }

        public SettingsEmbedBuilder WithGuild(IGuild guild)
        {
            _embed.WithFooter(guild.Name, guild.IconUrl);
            return this;
        }

        public SettingsEmbedBuilder AddSetting(string name, object value, string commandAlias)
            => AddSetting(name, value, _context.Command.Module.Type, commandAlias);

        public SettingsEmbedBuilder AddSetting(string name, object value, Type moduleBaseType, string commandAlias)
        {
            if (!typeof(DiscordModuleBase).IsAssignableFrom(moduleBaseType))
            {
                throw new ArgumentException(
                    $"{moduleBaseType} must be derived from {typeof(DiscordModuleBase)}",
                    nameof(moduleBaseType));
            }

            var module = _commandService.GetAllModules().First(x => x.Type == moduleBaseType);
            var command = module.Commands.First(x => x.Aliases.FirstOrDefault() == commandAlias);

            var fieldNameText = new StringBuilder();
            var fieldValueText = new StringBuilder();

            if (value is bool boolValue)
            {
                fieldNameText.Append("<:");

                if (boolValue)
                    fieldNameText.Append(_config.GetValue<string>("Emotes:SettingsCheckMark"));
                else
                    fieldNameText.Append(_config.GetValue<string>("Emotes:SettingsCrossMark"));

                fieldNameText.Append("> ");
            }
            else
            {
                fieldValueText.AppendLine(value.ToString() ?? string.Empty);
            }

            fieldNameText.Append(name);

            fieldValueText
                .Append("> Use the **")
                .Append(command.FullAliases[0])
                .Append("** command to change this setting.");

            _embed.AddField(fieldNameText.ToString(), fieldValueText.ToString());

            return this;
        }

        public Embed Build() => _embed.Build();
    }
}
