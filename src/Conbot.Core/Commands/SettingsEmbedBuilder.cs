using System;
using System.Linq;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Disqord;
using Disqord.Bot;

using Qmmands;

namespace Conbot.Commands
{
    public class SettingsEmbedBuilder
    {
        private readonly CommandContext _context;
        private readonly IConfiguration _config;
        private readonly DiscordBotBase _bot;
        private readonly LocalEmbed _embed;

        public SettingsEmbedBuilder(CommandContext context)
        {
            _context = context;

            var services = _context.Services;
            _config = services.GetRequiredService<IConfiguration>();
            _bot = services.GetRequiredService<DiscordBotBase>();

            _embed = new LocalEmbed().WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")));
        }

        public SettingsEmbedBuilder WithTitle(string title)
        {
            _embed.WithTitle(title);
            return this;
        }

        public SettingsEmbedBuilder WithColor(Color color)
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
            _embed.WithFooter(guild.Name, guild.GetIconUrl());
            return this;
        }

        public SettingsEmbedBuilder AddSetting(string name, object value, string commandAlias)
            => AddSetting(name, value, _context.Command.Module.Type, commandAlias);

        public SettingsEmbedBuilder AddSetting(string name, object value, Type moduleBaseType, string commandAlias)
        {
            var module = _bot.Commands.GetAllModules().First(x => x.Type == moduleBaseType);
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

        public LocalEmbed Build() => _embed;
    }
}
