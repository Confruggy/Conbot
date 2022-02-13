using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Conbot.Commands;
using Conbot.Extensions;

using Disqord;
using Disqord.Extensions.Interactivity;
using Disqord.Rest;

namespace Conbot.Interactive;

public class Paginator
{
    private readonly List<(string?, LocalEmbed?)> _pages = new();

    public void AddPage(string? text, LocalEmbed? embedBuilder) => _pages.Add((text, embedBuilder));

    public void AddPage(LocalEmbed embedBuilder) => AddPage(null, embedBuilder);

    public void AddPage(string text) => AddPage(text, null);

    public LocalInteractiveMessage ToInteractiveMessage(ConbotCommandContext context, int startIndex = 0,
        bool reply = true)
    {
        if (startIndex >= _pages.Count || startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        return ToInteractiveMessageInternal(context, startIndex, reply);
    }

    private LocalInteractiveMessage ToInteractiveMessageInternal(ConbotCommandContext context, int startIndex = 0,
        bool reply = true)
    {
        var config = context.Services.GetRequiredService<IConfiguration>();

        int currentIndex = startIndex;
        var start = _pages[currentIndex];

        var localMessage = new LocalInteractiveMessage()
            .WithContent(start.Item1)
            .WithEmbeds(start.Item2)
            .WithPrecondition(x => x.Id == context.Author.Id);

        if (reply)
            localMessage.WithReply(context.Message.Id, context.ChannelId, context.GuildId);

        if (_pages.Count > 2)
        {
            localMessage.AddReactionCallback(config.GetValue<string>("Emotes:First"), x => x
                .WithCallback(async (msg, _) =>
                {
                    if (currentIndex != 0)
                    {
                        currentIndex = 0;

                        var page = _pages[currentIndex];
                        await msg.ModifyAsync(m =>
                        {
                            m.Content = page.Item1;
                            m.Embeds = page.Item2 is null ? null : new[] { page.Item2 };
                        });
                    }
                }));
        }

        localMessage
            .AddReactionCallback(config.GetValue<string>("Emotes:Backward"), x => x
                .WithCallback(async (msg, _) =>
                {
                    if (currentIndex > 0)
                    {
                        currentIndex--;

                        var page = _pages[currentIndex];
                        await msg.ModifyAsync(m =>
                        {
                            m.Content = page.Item1;
                            m.Embeds = page.Item2 is null ? null : new[] { page.Item2 };
                        });
                    }
                }))
            .AddReactionCallback(config.GetValue<string>("Emotes:Forward"), x => x
                .WithCallback(async (msg, _) =>
                {
                    if (currentIndex < _pages.Count - 1)
                    {
                        currentIndex++;

                        var page = _pages[currentIndex];
                        await msg.ModifyAsync(m =>
                        {
                            m.Content = page.Item1;
                            m.Embeds = page.Item2 is null ? null : new[] { page.Item2 };
                        });
                    }
                }));

        if (_pages.Count > 2)
        {
            localMessage
                .AddReactionCallback(config.GetValue<string>("Emotes:Last"), x => x
                    .WithCallback(async (msg, _) =>
                    {
                        if (currentIndex != _pages.Count - 1)
                        {
                            currentIndex = _pages.Count - 1;

                            var page = _pages[currentIndex];

                            await msg.ModifyAsync(m =>
                            {
                                m.Content = page.Item1;
                                m.Embeds = page.Item2 is null ? null : new[] { page.Item2 };
                            });
                        }
                    }));
        }

        if (_pages.Count > 3)
        {
            localMessage
                .AddReactionCallback(config.GetValue<string>("Emotes:GoToPage"), x => x
                    .WithCallback(async (msg, _) =>
                    {
                        var message = await context.Bot.SendMessageAsync(msg.ChannelId,
                            new LocalMessage().WithContent("Enter the page you want to go to"));

                        var response = await context.WaitForMessageAsync();

                        var tasks = new List<Task> { message.TryDeleteAsync() };

                        if (response?.Message is not IUserMessage responseMessage)
                            return;

                        if (!int.TryParse(responseMessage.Content, out int pageIndex))
                        {
                            await context.Bot.SendMessageAsync(msg.ChannelId,
                                new LocalMessage().WithContent("Invalid input."));
                            return;
                        }

                        if (pageIndex < 1 || pageIndex > _pages.Count)
                        {
                            await context.Bot.SendMessageAsync(msg.ChannelId,
                                new LocalMessage().WithContent("This page doesn't exist."));
                            return;
                        }

                        tasks.Add(responseMessage.TryDeleteAsync());

                        currentIndex = pageIndex - 1;

                        var page = _pages[currentIndex];
                        await message.ModifyAsync(m =>
                        {
                            m.Content = page.Item1;
                            m.Embeds = page.Item2 is null ? null : new[] { page.Item2 };
                        });

                        await Task.WhenAll(tasks);
                    }));
        }

        localMessage.AddReactionCallback(config.GetValue<string>("Emotes:Stop"), x => x
            .WithCallback((msg, _) => msg.Stop()));

        return localMessage;
    }
}