using System.Threading.Tasks;

using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;

namespace Conbot.RpsPlugin
{
    public class RpsMenu : InteractiveMenu
    {
        private readonly Snowflake _user1Id;
        private readonly Snowflake _user2Id;

        public RpsMenu(RpsView view)
            : base(view.User1State.User.Id, view)
        {
            _user1Id = view.User1State.User.Id;
            _user2Id = view.User2State.User.Id;
        }

        protected override ValueTask<bool> CheckInteractionAsync(InteractionReceivedEventArgs e)
            => ValueTask.FromResult(e.AuthorId == _user1Id || e.AuthorId == _user2Id);
    }
}
