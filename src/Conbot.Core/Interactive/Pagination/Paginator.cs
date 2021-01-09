using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Conbot.Commands;
using Conbot.Extensions;

using Discord;
using Discord.Rest;

namespace Conbot.Interactive
{
    public class Paginator
    {
        private readonly List<(string?, Embed?)> _pages = new();

        public void AddPage(string? text, Embed? embed) => _pages.Add((text, embed));

        public void AddPage(Embed embed) => AddPage(null, embed);

        public void AddPage(string text) => AddPage(text, null);

        public Task<IUserMessage> RunAsync(InteractiveService service, DiscordCommandContext context,
            int startIndex = 0, bool reply = true)
        {
            if (startIndex >= _pages.Count || startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            return RunInternalAsync(service, context, startIndex, reply);
        }

        private async Task<IUserMessage> RunInternalAsync(InteractiveService service, DiscordCommandContext context,
            int startIndex = 0, bool reply = true)
        {
            var config = context.ServiceProvider.GetRequiredService<IConfiguration>();

            int currentIndex = startIndex;
            var start = _pages[currentIndex];

            RestUserMessage message;
            if (context.Interaction != null)
            {
                message = (RestUserMessage)await context.Interaction.RespondAsync(start.Item1, embed: start.Item2);
            }
            else
            {
                message = await context.Channel.SendMessageAsync(
                    start.Item1,
                    embed: start.Item2,
                    allowedMentions: AllowedMentions.None,
                    messageReference: reply ? new MessageReference(context.Message!.Id) : null);
            }

            if (_pages.Count <= 1)
                return message;

            var builder = new InteractiveMessageBuilder()
                .WithPrecondition(x => x.Id == context.User.Id);

            if (_pages.Count > 2)
            {
                builder.AddReactionCallback(config.GetValue<string>("Emotes:First"), x => x
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
                .AddReactionCallback(config.GetValue<string>("Emotes:Backward"), x => x
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
                .AddReactionCallback(config.GetValue<string>("Emotes:Forward"), x => x
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
                    .AddReactionCallback(config.GetValue<string>("Emotes:Last"), x => x
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
                    .AddReactionCallback(config.GetValue<string>("Emotes:GoToPage"), x => x
                        .WithCallback(async _ =>
                        {
                            var msg = await message.Channel.SendMessageAsync("To which page do you want to go?");

                            var respond = await context.WaitForMessageAsync();

                            var tasks = new List<Task> { msg.TryDeleteAsync() };

                            if (respond == null)
                                return;

                            if (!int.TryParse(respond.Content, out int pageIndex))
                            {
                                await msg.Channel.SendMessageAsync("Invalid input.");
                                return;
                            }

                            if (pageIndex < 1 || pageIndex > _pages.Count)
                            {
                                await msg.Channel.SendMessageAsync("This page doesn't exist.");
                                return;
                            }

                            tasks.Add(respond.TryDeleteAsync());

                            currentIndex = pageIndex - 1;

                            var page = _pages[currentIndex];
                            await message.ModifyAsync(m =>
                            {
                                m.Content = page.Item1;
                                m.Embed = page.Item2;
                            });

                            await Task.WhenAll(tasks);
                        })
                        .ShouldResumeAfterExecution(true));
            }

            builder.AddReactionCallback(config.GetValue<string>("Emotes:Stop"), x => x.ShouldResumeAfterExecution(false));

            await service.ExecuteInteractiveMessageAsync(builder.Build(), message, context.User);
            return message;
        }
    }
}
