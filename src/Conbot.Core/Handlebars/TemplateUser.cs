using Disqord;

namespace Conbot
{
    public class TemplateUser
    {
        private readonly IUser _user;

        //User
        public string Discrim => _user.Discriminator;
        public string Discriminator => _user.Discriminator;
        public ulong Id => _user.Id.RawValue;
        public bool IsBot => _user.IsBot;
        public string Mention => _user.Mention;
        public string Name => _user.Name;
        public string Tag => _user.Tag;

        //Member
        public long? BoostedAt => (_user as IMember)?.BoostedAt?.ToUnixTimeSeconds();
        public bool? IsDeafened => (_user as IMember)?.IsDeafened;
        public bool? IsMuted => (_user as IMember)?.IsMuted;
        public bool? IsPending => (_user as IMember)?.IsPending;
        public long? JoinedAt => (_user as IMember)?.JoinedAt.GetValueOrNullable()?.ToUnixTimeSeconds();
        public string Nick => (_user as IMember)?.Nick ?? _user.Name;
        public long? TimeOutUntil => (_user as IMember)?.TimedOutUntil?.ToUnixTimeSeconds();

        public TemplateUser(IUser user) => _user = user;

        public override string ToString() => _user.ToString()!;
    }
}
