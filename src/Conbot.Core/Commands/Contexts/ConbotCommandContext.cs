using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.Extensions.DependencyInjection;

using Disqord;
using Disqord.Bot;
using Disqord.Gateway;

namespace Conbot.Commands;

public class ConbotCommandContext : DiscordCommandContext
{
    private readonly List<IUserMessage> _messages;

    public override ConbotBot Bot => (ConbotBot)base.Bot;
    public override IGatewayUserMessage Message => (IGatewayUserMessage)_messages[0];
    public ReadOnlyCollection<IUserMessage> Messages => _messages.AsReadOnly();

    public ConbotCommandContext(ConbotBot bot, IPrefix prefix, string input, IGatewayUserMessage message,
        IServiceProvider services)
        : base(bot, prefix, input, message, services)
    {
        _messages = new List<IUserMessage> { message };
    }

    public ConbotCommandContext(ConbotBot bot, IPrefix prefix, string input, IGatewayUserMessage message,
        IServiceScope serviceScope)
        : base(bot, prefix, input, message, serviceScope)
    {
        _messages = new List<IUserMessage> { message };
    }

    public void AddMessage(IUserMessage message) => _messages.Add(message);
}