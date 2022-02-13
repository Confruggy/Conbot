using System;
using System.Threading.Tasks;

using Disqord;
using Disqord.Extensions.Interactivity.Menus;

using Humanizer;

namespace Conbot.RpsPlugin;

public class RpsView : ViewBase
{
    private readonly Random _random;

    public RpsUserState User1State { get; }
    public RpsUserState User2State { get; }
    public int Draws { get; set; }

    public ButtonViewComponent RockButton { get; }
    public ButtonViewComponent PaperButton { get; }
    public ButtonViewComponent ScissorsButton { get; }
    public ButtonViewComponent RematchButton { get; }

    public RpsView(IUser user1, IUser user2, Random random)
        : base(new LocalMessage())
    {
        User1State = new RpsUserState(user1);
        User2State = new RpsUserState(user2);

        _random = random;

        RockButton = new ButtonViewComponent(OnEmojiButtonAsync)
        {
            Emoji = new LocalEmoji("✊"),
            Style = LocalButtonComponentStyle.Secondary
        };

        AddComponent(RockButton);

        PaperButton = new ButtonViewComponent(OnEmojiButtonAsync)
        {
            Emoji = new LocalEmoji("✋"),
            Style = LocalButtonComponentStyle.Secondary
        };

        AddComponent(PaperButton);

        ScissorsButton = new ButtonViewComponent(OnEmojiButtonAsync)
        {
            Emoji = new LocalEmoji("✌️"),
            Style = LocalButtonComponentStyle.Secondary
        };

        AddComponent(ScissorsButton);

        RematchButton = new ButtonViewComponent(OnRematchButtonAsync)
        {
            Label = "Rematch",
            Style = LocalButtonComponentStyle.Primary,
            IsDisabled = true
        };

        AddComponent(RematchButton);

        if (User2State.User.IsBot)
            User2State.SelectedButton = new[] { RockButton, PaperButton, ScissorsButton }[_random.Next(0, 3)];

        ApplyStartContent();
    }

    public ValueTask OnEmojiButtonAsync(ButtonEventArgs e)
    {
        if (e.Member.Id == User1State.User.Id && User1State.SelectedButton is null)
        {
            User1State.SelectedButton = e.Button;
            ReportChanges();
        }
        else if (e.Member.Id == User2State.User.Id && User2State.SelectedButton is null)
        {
            User2State.SelectedButton = e.Button;
            ReportChanges();
        }

        return default;
    }

    public ValueTask OnRematchButtonAsync(ButtonEventArgs e)
    {
        RockButton.IsDisabled = false;
        RockButton.Style = LocalButtonComponentStyle.Secondary;

        PaperButton.IsDisabled = false;
        PaperButton.Style = LocalButtonComponentStyle.Secondary;

        ScissorsButton.IsDisabled = false;
        ScissorsButton.Style = LocalButtonComponentStyle.Secondary;

        RematchButton.IsDisabled = true;

        User1State.SelectedButton = null;
        User2State.SelectedButton = null;

        if (User2State.User.IsBot)
        {
            User2State.SelectedButton
                = new[] { RockButton, PaperButton, ScissorsButton }[_random.Next(0, 3)];
        }

        TemplateMessage = TemplateMessage.WithAllowedMentions(LocalAllowedMentions.None);
        ApplyStartContent();

        return default;
    }

    public override ValueTask UpdateAsync()
    {
        if (User1State.SelectedButton is not null && User2State.SelectedButton is null)
        {
            TemplateMessage = TemplateMessage
                .WithContent(
                    $"{User1State.User.Mention} chose their option. {User2State.User.Mention}, your turn!");
        }
        else if (User1State.SelectedButton is null && User2State.SelectedButton is not null)
        {
            TemplateMessage = TemplateMessage
                .WithContent(
                    $"{User2State.User.Mention} chose their option. {User1State.User.Mention}, your turn!");
        }
        else if (User1State.SelectedButton is not null && User2State.SelectedButton is not null)
        {
            string text;
            LocalAllowedMentions allowedMentions;

            switch (CheckOptions(User1State.SelectedButton.Emoji.Name, User2State.SelectedButton.Emoji.Name))
            {
                case true:
                    text = $"{User1State.User.Mention} wins!";

                    User1State.SelectedButton.Style = LocalButtonComponentStyle.Success;
                    User2State.SelectedButton.Style = LocalButtonComponentStyle.Danger;

                    allowedMentions = new LocalAllowedMentions().WithUserIds(User1State.User.Id);

                    User1State.Wins++;

                    break;
                case false:
                    text = $"{User2State.User.Mention} wins!";

                    User2State.SelectedButton.Style = LocalButtonComponentStyle.Success;
                    User1State.SelectedButton.Style = LocalButtonComponentStyle.Danger;

                    allowedMentions = new LocalAllowedMentions().WithUserIds(User2State.User.Id);

                    User2State.Wins++;

                    break;
                default:
                    text = "Draw.";

                    User1State.SelectedButton.Style = LocalButtonComponentStyle.Primary;

                    allowedMentions = LocalAllowedMentions.None;

                    Draws++;

                    break;
            }

            RockButton.IsDisabled = true;
            PaperButton.IsDisabled = true;
            ScissorsButton.IsDisabled = true;

            RematchButton.IsDisabled = false;

            TemplateMessage = TemplateMessage
                .WithContent(text)
                .WithAllowedMentions(allowedMentions);
        }

        return default;
    }

    public override LocalMessage ToLocalMessage()
        => base.ToLocalMessage()
            .WithEmbeds(CreateEmbed());

    private void ApplyStartContent()
        => TemplateMessage = TemplateMessage.WithContent(
            $"{User1State.User.Mention} and {User2State.User.Mention}, choose your option!");

    private LocalEmbed CreateEmbed()
    {
        string user1Name;
        string user2Name;

        if (User1State.User is IMember member1)
            user1Name = member1.Nick ?? member1.Name;
        else
            user1Name = User1State.User.Name;

        if (User2State.User is IMember member2)
            user2Name = member2.Nick ?? member2.Name;
        else
            user2Name = User2State.User.Name;

        return new LocalEmbed()
            .WithTitle("Rock Paper Scissors")
            .WithColor(new Color(0xFFDC5D))
            .AddField(user1Name, "win".ToQuantity(User1State.Wins, Markdown.Bold("0")), true)
            .AddField(user2Name, "win".ToQuantity(User2State.Wins, Markdown.Bold("0")), true)
            .AddField("Draws", "draw".ToQuantity(Draws, Markdown.Bold("0")), true);
    }

    private static bool? CheckOptions(string option1, string option2)
        => option1 switch
        {
            "✊" => option2 switch
            {
                "✌️" => true,
                "✋" => false,
                _ => null
            },
            "✌️" => option2 switch
            {
                "✊" => false,
                "✋" => true,
                _ => null
            },
            "✋" => option2 switch
            {
                "✊" => true,
                "✌️" => false,
                _ => null
            },
            _ => null
        };
}