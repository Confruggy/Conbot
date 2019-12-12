using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Conbot.Extensions;
using Conbot.InteractiveMessages;
using Discord;
using Discord.Commands;

namespace Conbot.Pagination
{
    public class Paginator
    {
        private readonly List<(string, Embed)> _pages = new List<(string, Embed)>();

        public void AddPage(string text, Embed embed) => _pages.Add((text, embed));

        public void AddPage(Embed embed) => AddPage("", embed);

        public void AddPage(string text) => AddPage(text, null);

        public async Task<IUserMessage> RunAsync(SocketCommandContext context, int startIndex = 0)
        {
            if (startIndex >= _pages.Count || startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            int currentIndex = startIndex;
            var start = _pages[currentIndex];

            var message = await context.Channel.SendMessageAsync(start.Item1, embed: start.Item2).ConfigureAwait(false);

            if (_pages.Count <= 1)
                return message;

            var builder = new InteractiveMessageBuilder()
                .WithPrecondition(x => x.Id == context.User.Id);

            if (_pages.Count > 2)
                builder.AddReactionCallback(x => x
                    .WithEmote("first:654781462490644501")
                    .WithCallback(async r =>
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

            builder
                .AddReactionCallback(x => x
                    .WithEmote("backward:654781463027515402")
                    .WithCallback(async r =>
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
                    .WithEmote("forward:654781462402301964")
                    .WithCallback(async r =>
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
                builder
                    .AddReactionCallback(x => x
                        .WithEmote("last:654781462373203981")
                        .WithCallback(async r =>
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

            if (_pages.Count > 3)
                builder
                    .AddReactionCallback(x => x
                        .WithEmote("go_to_page:654781462603628544")
                        .WithCallback(async r =>
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

            builder.AddReactionCallback(x => x.WithEmote("stop:654781462385655849").ShouldResumeAfterExecution(false));

            await builder.Build().ExecuteAsync(context.Client, message).ConfigureAwait(false);

            return message;
        }
    }
}