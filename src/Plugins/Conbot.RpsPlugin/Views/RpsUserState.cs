using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Conbot.RpsPlugin;

public class RpsUserState
{
    public IUser User { get; }
    public ButtonViewComponent? SelectedButton { get; set; }
    public int Wins { get; set; }

    public RpsUserState(IUser user) => User = user;
}