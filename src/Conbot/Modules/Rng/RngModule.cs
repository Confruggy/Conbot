using System;
using System.Threading.Tasks;
using Conbot.Commands;
using Qmmands;

namespace Conbot.Modules.Rng
{
    [Name("RNG")]
    [Description("Random number generation.")]
    public class RngModule : DiscordModuleBase
    {
        private readonly Random _random;

        public RngModule(Random random) => _random = random;

        [Group("roll")]
        [Description("Rolls a random number.")]
        public class RollCommands : DiscordModuleBase
        {
            private readonly Random _random;

            public RollCommands(Random random) => _random = random;

            [Command]
            [Description("Rolls a random number between 1 and a maximal value.")]
            public Task RollAsync([Description("The maximal value.")] int max = 100)
                => RollAsync(1, max);

            [Command]
            [Description("Rolls a random number between a minimal and a maximal value.")]
            public async Task RollAsync(
                [Description("The minimal value.")] int min,
                [Description("The maximal value.")] int max)
            {
                int number = _random.Next(min, max);
                await ReplyAsync($"{Context.User.Mention} rolled **{number}**.");
            }
        }

        [Command("choose")]
        [Description("Chooses an option from several options.")]
        public async Task ChooseAsync(
            [Description("The options to choose from."), MinLength(2)] params string[] options)
            => await ReplyAsync(options[_random.Next(options.Length)]);

        [Command("8ball")]
        [Description("Asks the Magic 8 Ball a question.")]
        public Task EightBallAsync(
            [Description("The question to ask."), Remainder] string question)
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