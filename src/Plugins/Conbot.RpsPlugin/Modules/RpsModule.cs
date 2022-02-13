using System;

using Conbot.Commands;

using Disqord;
using Disqord.Bot;

using Qmmands;

namespace Conbot.RpsPlugin;

[Name("Rock Paper Scissors")]
[Description("Play Rock Paper Scissors.")]
[Group("rps")]
public class RpsModule : ConbotModuleBase
{
    private readonly Random _random;

    public RpsModule(Random random) => _random = random;

    [Command]
    [Description("Starts a Rock Paper Scissors match against a member or the bot.")]
    public DiscordCommandResult Rps(
        [Description("The opponent to play against.")]
        [Remarks("If no opponent is entered, you will play against the bot instead.")]
        IMember? opponent = null)
    {
        if (opponent?.Id == Context.Author.Id)
            return Fail("You can't play against yourself.");

        return Menu(
            new RpsMenu(new RpsView(Context.Author, opponent as IUser ?? Context.Bot.CurrentUser, _random)));
    }
}