using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Extensions;
using Conbot.InteractiveMessages;
using Discord;
using Discord.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Conbot.Services.Interactive
{
    public class Paginator
    {
        private readonly List<(string, Embed)> _pages = new List<(string, Embed)>();

        public void AddPage(string text, Embed embed) => _pages.Add((text, embed));

        public void AddPage(Embed embed) => AddPage("", embed);

        public void AddPage(string text) => AddPage(text, null);

        public async Task<IUserMessage> RunAsync(InteractiveService service, DiscordCommandContext context,
            int startIndex = 0, bool reply = true)
        {
            var config = context.ServiceProvider.GetRequiredService<IConfiguration>();

            if (startIndex >= _pages.Count || startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            int currentIndex = startIndex;
            var start = _pages[currentIndex];

            var message = context.Interaction != null
                ? await context.Interaction.RespondAsync(
                    start.Item1,
                    embed: start.Item2)
                    .ConfigureAwait(false) as RestUserMessage
                : await context.Channel.SendMessageAsync(
                    start.Item1,
                    embed: start.Item2,
                    allowedMentions: AllowedMentions.None,
                    messageReference: reply ? new MessageReference(context.Message.Id) : null)
                    .ConfigureAwait(false);

            if (_pages.Count <= 1)
                return message;

            var builder = new InteractiveMessageBuilder()
                .WithPrecondition(x => x.Id == context.User.Id);

            if (_pages.Count > 2)
            {
                builder.AddReactionCallback(x => x
                    .WithEmote(config.GetValue<string>("Emotes:First"))
                    .WithCallback(async _ =>
                    {
                        if (currentIndex != 0)
                        {
                            currentIndex = 0;

                            var page = _pages[currentIndex];
                            await message.ModifyAsync(m =>
                            {
                                m.Content = page.Item1;
                                m.Embed = page.Item2;
                            });
                        }
                    })
                    .ShouldResumeAfterExecution(true));
            }

            builder
                .AddReactionCallback(x => x
                    .WithEmote(config.GetValue<string>("Emotes:Backward"))
                    .WithCallback(async _ =>
                    {
                        if (currentIndex > 0)
                        {
                            currentIndex--;

                            var page = _pages[currentIndex];
                            await message.ModifyAsync(m =>
                            {
                                m.Content = page.Item1;
                                m.Embed = page.Item2;
                            });
                        }
                    })
                    .ShouldResumeAfterExecution(true))
                .AddReactionCallback(x => x
                    .WithEmote(config.GetValue<string>("Emotes:Forward"))
                    .WithCallback(async _ =>
                    {
                        if (currentIndex < _pages.Count - 1)
                        {
                            currentIndex++;

                            var page = _pages[currentIndex];
                            await message.ModifyAsync(m =>
                            {
                                m.Content = page.Item1;
                                m.Embed = page.Item2;
                            });
                        }
                    })
                    .ShouldResumeAfterExecution(true));

            if (_pages.Count > 2)
            {
                builder
                    .AddReactionCallback(x => x
                        .WithEmote(config.GetValue<string>("Emotes:Last"))
                        .WithCallback(async _ =>
                        {
                            if (currentIndex != _pages.Count - 1)
                            {
                                currentIndex = _pages.Count - 1;

                                var page = _pages[currentIndex];

                                await message.ModifyAsync(m =>
                                {
                                    m.Content = page.Item1;
                                    m.Embed = page.Item2;
                                });
                            }
                        })
                        .ShouldResumeAfterExecution(true));
            }

            if (_pages.Count > 3)
            {
                builder
                    .AddReactionCallback(x => x
                        .WithEmote(config.GetValue<string>("Emotes:GoToPage"))
                        .WithCallback(async _ =>
                        {
                            var msg = await message.Channel.SendMessageAsync("To which page do you want to go?")
                                .ConfigureAwait(false);

                            var respond = await context.WaitForMessageAsync();

                            var tasks = new List<Task>
                            {
                                msg.TryDeleteAsync()
                            };

                            if (respond == null)
                                return;

                            if (!int.TryParse(respond.Content, out int pageIndex))
                            {
                                await msg.Channel.SendMessageAsync("Invalid input.").ConfigureAwait(false);
                                return;
                            }

                            if (pageIndex < 1 || pageIndex > _pages.Count)
                            {
                                await msg.Channel.SendMessageAsync("This page doesn't exist.").ConfigureAwait(false);
                                return;
                            }

                            tasks.Add(respond.TryDeleteAsync());

                            currentIndex = pageIndex - 1;

                            var page = _pages[currentIndex];
                            await message.ModifyAsync(m =>
                            {
                                m.Content = page.Item1;
                                m.Embed = page.Item2;
                            }).ConfigureAwait(false);

                            await Task.WhenAll(tasks).ConfigureAwait(false);
                        }).ShouldResumeAfterExecution(true));
            }

            builder.AddReactionCallback(x => x.WithEmote(config.GetValue<string>("Emotes:Stop")).ShouldResumeAfterExecution(false));

            await service.ExecuteInteractiveMessageAsync(builder.Build(), message, context.User).ConfigureAwait(false);
            return message;
        }
    }
}