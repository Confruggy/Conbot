using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Conbot.Commands;

using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;

using Qmmands;

using Qommon.Collections;

namespace Conbot.HelpPlugin;

public class CommandView : ViewBase
{
    private readonly IReadOnlyCollection<Command> _overloads;
    private ConbotCommandContext? _newContext;

    public Command Command { get; }
    public ConbotCommandContext Context { get; }
    public IConfiguration Configuration { get; }
    public ModuleView? ParentView { get; }
    public StartView? StartView { get; }

    public SelectionViewComponent? OverloadSelection { get; }
    public ButtonViewComponent? StartButton { get; }
    public ButtonViewComponent? BackButton { get; }
    public ButtonViewComponent TryButton { get; }
    public ButtonViewComponent StopButton { get; }

    public CommandView(Command command, ConbotCommandContext context, IConfiguration configuration,
        LocalMessage? templateMessage = null)
        : this(command, context, configuration, null, null, templateMessage)
    {
    }

    public CommandView(Command command, ModuleView parentView, StartView? startView = null,
        LocalMessage? templateMessage = null)
        : this(command, parentView.Context, parentView.Configuration, parentView, startView, templateMessage)
    {
    }

    public CommandView(Command command, ConbotCommandContext context, IConfiguration configuration,
        ModuleView? parentView, StartView? startView, LocalMessage? templateMessage)
        : base(templateMessage)
    {
        Command = command;
        Context = context;
        Configuration = configuration;
        ParentView = parentView;
        StartView = startView;

        _overloads = GetOverloads().ToReadOnlyList();

        if (_overloads.Count != 0)
        {
            var options = _overloads.Select(overload => new LocalSelectionComponentOption()
                    .WithLabel(overload.Name)
                    .WithValue(overload.ToString())
                    .WithDescription(overload.Description ?? "No Description."))
                .ToList();

            OverloadSelection = new SelectionViewComponent(OnOverloadSelectionAsync)
            {
                Placeholder = "Select an overload for more information",
                Options = options,
                MaximumSelectedOptions = 1
            };

            AddComponent(OverloadSelection);
        }

        if (StartView is not null)
        {
            StartButton = new ButtonViewComponent(OnStartButtonAsync)
            {
                Label = "Start", Style = LocalButtonComponentStyle.Primary
            };

            AddComponent(StartButton);
        }

        if (ParentView is not null)
        {
            BackButton = new ButtonViewComponent(OnBackButtonAsync)
            {
                Label = "Back", Style = LocalButtonComponentStyle.Primary
            };

            AddComponent(BackButton);
        }

        TryButton = new ButtonViewComponent(OnTryButtonAsync)
        {
            Label = "Try", Style = LocalButtonComponentStyle.Secondary
        };

        AddComponent(TryButton);

        StopButton = new ButtonViewComponent(OnStopButtonAsync)
        {
            Emoji = LocalEmoji.FromString(Configuration["Emotes:CrossMark"]),
            Style = LocalButtonComponentStyle.Danger
        };

        AddComponent(StopButton);
    }

    private IEnumerable<Command> GetOverloads()
        => Command.Module.Commands
            .Where(x => x.FullAliases[0] == Command.FullAliases[0] && x != Command);

    public ValueTask OnOverloadSelectionAsync(SelectionEventArgs e)
    {
        var overload = _overloads.First(x => x.ToString() == e.SelectedOptions[0].Value);

        Menu.View = ParentView is not null
            ? new CommandView(overload, ParentView, StartView, TemplateMessage)
            : new CommandView(overload, Context, Configuration, TemplateMessage);

        return default;
    }

    public ValueTask OnStartButtonAsync(ButtonEventArgs e)
    {
        Menu.View = StartView;

        return default;
    }

    public ValueTask OnBackButtonAsync(ButtonEventArgs e)
    {
        Menu.View = ParentView;

        return default;
    }

    public ValueTask OnTryButtonAsync(ButtonEventArgs e)
    {
        if (_newContext is null)
            return default;

        var parser = Context.Bot.Commands.GetArgumentParser<InteractiveArgumentParser>();
        var handler = Context.Bot.Services.GetRequiredService<ICommandFailedHandler>();

        //this is a hack, don't recommend
        Context.Bot.Queue.Post(_newContext, async context =>
        {
            var parserResult = await parser.ParseAsync(context);

            var input = new StringBuilder()
                .Append(Command.FullAliases[0]);

            if (!parserResult.IsSuccessful)
            {
                var argumentParseFailedResult =
                    (ArgumentParseFailedResult)typeof(ArgumentParseFailedResult)
                        .GetConstructor(
                            BindingFlags.Instance | BindingFlags.NonPublic,
                            null,
                            new[] { typeof(CommandContext), typeof(ArgumentParserResult) },
                            null)!
                        .Invoke(new object[] { context, parserResult });

                await handler.HandleFailedResultAsync((ConbotCommandContext)context, argumentParseFailedResult);
            }
            else
            {
                if (parserResult.Arguments.Count != 0)
                    input.Append(' ').AppendJoin(' ', parserResult.Arguments.Select(x => x.Value));

                var commandResult = await Context.Bot.Commands.ExecuteAsync(input.ToString(), context);

                if (commandResult.IsSuccessful)
                    return;

                await handler.HandleFailedResultAsync((ConbotCommandContext)context, (FailedResult)commandResult);
            }

            await context.DisposeAsync();
        });

        _newContext = null;

        return UpdateAsync();
    }

    public async ValueTask OnStopButtonAsync(ButtonEventArgs e)
    {
        if (TemplateMessage is not null)
        {
            _ = e.Interaction.Message.ModifyAsync(x =>
            {
                x.Content = TemplateMessage.Content;
                x.Embeds = new Optional<IEnumerable<LocalEmbed>>(TemplateMessage.Embeds);
                x.Components = null;
            });
        }
        else
        {
            _ = e.Interaction.Message.DeleteAsync();
        }

        Menu.Stop();

        if (_newContext is not null)
            await _newContext.DisposeAsync();
    }

    public override async ValueTask UpdateAsync()
    {
        if (_newContext is null)
        {
            CachedMessageGuildChannel? channel = null;

            if (Context is ConbotGuildCommandContext guildCommandContext)
                channel = guildCommandContext.Channel;

            var context = Context.Bot.CreateCommandContext(
                Context.Prefix, Command.FullAliases[0], Context.Message, channel);

            var commandField = typeof(CommandContext)
                .GetField("<Command>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

            commandField!.SetValue(context, Command);

            _newContext = context;

            var result = await Command.RunChecksAsync(_newContext);

            if (!result.IsSuccessful)
                TryButton.IsDisabled = true;
        }
    }

    public override LocalMessage ToLocalMessage()
    {
        var message = base.ToLocalMessage()
            .AddEmbed(CreateCommandEmbed());

        if (message.Reference is null)
            message.WithReply(Context.Messages[^1].Id, Context.ChannelId, Context.GuildId);

        return message;
    }

    public LocalEmbed CreateCommandEmbed()
    {
        var embed = new LocalEmbed()
            .WithAuthor(HelpUtils.GetPath(Command))
            .WithTitle($"{Command.FullAliases[0]} {HelpUtils.FormatParameters(Command)}")
            .WithColor(new Color(Configuration.GetValue<int>("DefaultEmbedColor")));

        var descriptionText = new StringBuilder()
            .AppendLine(Command.Description ?? "No Description.");

        if (!string.IsNullOrEmpty(Command.Remarks))
        {
            descriptionText
                .AppendLine()
                .Append(">>> ")
                .Append(Command.Remarks);
        }

        embed.WithDescription(descriptionText.ToString());

        var parameters = Command.Parameters;
        if (parameters.Count > 0)
        {
            var parameterText = new StringBuilder();

            foreach (var parameter in Command.Parameters)
            {
                parameterText
                    .AppendLine(HelpUtils.FormatParameter(parameter, true))
                    .Append("> ")
                    .Append(parameter.Description ?? "No Description.");

                if (parameter.Checks.FirstOrDefault(x => x is MinLengthAttribute)
                    is MinLengthAttribute minLengthCheck)
                {
                    parameterText
                        .Append(" Minimal ")
                        .Append(parameter.IsMultiple ? "amount" : "length")
                        .Append(" is ")
                        .Append(minLengthCheck.Length)
                        .Append('.');
                }

                if (parameter.Checks.FirstOrDefault(x => x is MaxLengthAttribute)
                    is MaxLengthAttribute maxLengthCheck)
                {
                    parameterText
                        .Append(" Maximal ")
                        .Append(parameter.IsMultiple ? "amount" : "length")
                        .Append(" is ")
                        .Append(maxLengthCheck.Length)
                        .Append('.');
                }

                if (parameter.Checks.FirstOrDefault(x => x is MinValueAttribute)
                    is MinValueAttribute minValueCheck)
                {
                    parameterText
                        .Append(" Minimal value is ")
                        .Append(minValueCheck.MinValue)
                        .Append('.');
                }

                if (parameter.Checks.FirstOrDefault(x => x is MaxValueAttribute)
                    is MaxValueAttribute maxValueCheck)
                {
                    parameterText
                        .Append(" Maximal value is ")
                        .Append(maxValueCheck.MaxValue)
                        .Append('.');
                }

                if (parameter.DefaultValue is not null && parameter.DefaultValue is not Array)
                {
                    parameterText.Append(" Default value is ")
                        .Append(parameter.DefaultValue)
                        .Append('.');
                }

                if (!string.IsNullOrEmpty(parameter.Remarks))
                {
                    parameterText
                        .Append(' ')
                        .Append(parameter.Remarks.Replace("\n", "\n> "));
                }

                parameterText.AppendLine();
            }

            embed.AddField("Parameters", parameterText);
        }

        string permissionsText = GetPermissionsText(Command);
        if (!string.IsNullOrEmpty(permissionsText))
            embed.AddField("Permissions", permissionsText);

        var overloadsText = new StringBuilder();
        if (_overloads.Any())
        {
            foreach (var overload in _overloads)
                overloadsText.AppendLine(HelpUtils.GetShortCommand(overload));

            embed.AddField("Overloads", overloadsText);
        }

        if (Command.FullAliases.Count > 1)
        {
            var aliasesText = new StringBuilder();

            foreach (string? alias in Command.FullAliases.Skip(1))
            {
                aliasesText
                    .Append(Markdown.Bold(alias))
                    .Append(' ')
                    .AppendLine(HelpUtils.FormatParameters(Command));
            }

            embed
                .AddField("Aliases", aliasesText)
                .WithFooter("Aliases are only supported by text commands.");
        }

        return embed;
    }

    private static string GetPermissionsText(Command command)
    {
        List<RequireAuthorGuildPermissionsAttribute> authorGuildPermissions = new();
        List<RequireAuthorChannelPermissionsAttribute> authorChannelPermissions = new();
        List<RequireBotGuildPermissionsAttribute> botGuildPermissions = new();
        List<RequireBotChannelPermissionsAttribute> botChannelPermissions = new();

        var module = command.Module;
        while (module is not null)
        {
            authorGuildPermissions.AddRange(module.Checks.OfType<RequireAuthorGuildPermissionsAttribute>());
            authorChannelPermissions.AddRange(module.Checks.OfType<RequireAuthorChannelPermissionsAttribute>());
            botGuildPermissions.AddRange(module.Checks.OfType<RequireBotGuildPermissionsAttribute>());
            botChannelPermissions.AddRange(module.Checks.OfType<RequireBotChannelPermissionsAttribute>());

            module = module.Parent;
        }

        authorGuildPermissions.AddRange(command.Checks.OfType<RequireAuthorGuildPermissionsAttribute>());
        authorChannelPermissions.AddRange(command.Checks.OfType<RequireAuthorChannelPermissionsAttribute>());
        botGuildPermissions.AddRange(command.Checks.OfType<RequireBotGuildPermissionsAttribute>());
        botChannelPermissions.AddRange(command.Checks.OfType<RequireBotChannelPermissionsAttribute>());

        var permissionsText = new StringBuilder();

        foreach (var authorGuildPermission in authorGuildPermissions)
        {
            permissionsText.AppendLine(
                RequirePermissionUtils.CreateRequirePermissionErrorReason(authorGuildPermission.Permissions));
        }

        foreach (var authorChannelPermission in authorChannelPermissions)
        {
            permissionsText.AppendLine(
                RequirePermissionUtils.CreateRequirePermissionErrorReason(authorChannelPermission.Permissions));
        }

        foreach (var botGuildPermission in botGuildPermissions)
        {
            permissionsText.AppendLine(
                RequirePermissionUtils.CreateRequirePermissionErrorReason(botGuildPermission.Permissions, true));
        }

        foreach (var botChannelPermission in botChannelPermissions)
        {
            permissionsText.AppendLine(
                RequirePermissionUtils.CreateRequirePermissionErrorReason(botChannelPermission.Permissions, true));
        }

        return permissionsText.ToString();
    }
}