using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Conbot.Modules.Rng
{
    [Name("RNG")]
    [Summary("Random number generation.")]
    public class RngModule : ModuleBase
    {
        private readonly Random _random;

        public RngModule(Random random) => _random = random;

        [Group("roll")]
        [Summary("Rolls a random number.")]
        public class RollCommands : ModuleBase
        {
            private readonly Random _random;

            public RollCommands(Random random) => _random = random;

            [Command]
            [Summary("Rolls a random number between 1 and a maximal value.")]
            public Task RollAsync([Summary("The maximal value.")] int max = 100)
                => RollAsync(1, max);

            [Command]
            [Summary("Rolls a random number between a minimal and a maximal value.")]
            public async Task RollAsync(
                [Summary("The minimal value.")] int min,
                [Summary("The maximal value.")] int max)
            {
                int number = _random.Next(min, max);
                await ReplyAsync($"{Context.User.Mention} rolled **{number}**.");
            }
        }

        [Command("choose")]
        [Summary("Lets the bot choose from several options.")]
        public async Task ChooseAsync(
            [Summary("The options to choose from.")] params string[] options)
        {
            if (options.Length < 2)
            {
                await ReplyAsync("You must enter at least 2 options.");
                return;
            }

            await ReplyAsync(options[_random.Next(options.Length)]);
        }

        [Command("8ball")]
        [Summary("Asks the Magic 8 Ball a question.")]
        public Task EightBallAsync(
            [Summary("The question to ask."), Remainder] string question)
        {
            string[] answers =
            {
                "It is certain.",
                "It is decidedly so.",
                "Without a doubt.",
                "Yes, definitely.",
                "You may rely on it.",
                "As I see it, yes.",
                "Most likely.",
                "Outlook good.",
                "Yes.",
                "Signs point to yes.",
                "Reply hazy try again.",
                "Ask again later.",
                "Better not tell you now.",
                "Cannot predict now.",
                "Concentrate and ask again.",
                "Don't count on it.",
                "My reply is no.",
                "My sources say no.",
                "Outlook not so good.",
                "Very doubtful."
            };
            return ReplyAsync($"🎱 {answers[_random.Next(answers.Length)]}");
        }
    }
}