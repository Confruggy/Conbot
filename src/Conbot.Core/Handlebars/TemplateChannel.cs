using Disqord;

namespace Conbot
{
    public class TemplateChannel
    {
        private readonly IChannel _channel;

        //Channel
        public ulong Id => _channel.Id;
        public string Name => _channel.Name;

        //Guild Channel
        public string? Mention => (_channel as IGuildChannel)?.Mention;
        public int? Position => (_channel as IGuildChannel)?.Position;

        public TemplateChannel(IChannel channel) => _channel = channel;

        public override string ToString() => _channel.ToString()!;
    }
}
