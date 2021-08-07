using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Disqord;

namespace Conbot.Interactive
{
    public class InteractiveUserMessage : TransientUserMessage, IInteractiveUserMessage
    {
        private readonly InteractiveService _service;
        private readonly Lazy<CancellationTokenSource> _tokenSource;

        public Func<IUser, Task<bool>>? Precondition { get; }
        public int Timeout { get; } = 600000;
        public IReadOnlyDictionary<LocalEmoji, ReactionCallback> ReactionCallbacks { get; }
        public IReadOnlyCollection<MessageCallback> MessageCallbacks { get; }
        public IUser User { get; }
        public DateTimeOffset TimeoutsAt { get; internal set; }

        public CancellationTokenSource TokenSource => _tokenSource.Value;

        internal InteractiveUserMessage(TransientUserMessage message, LocalInteractiveMessage interactiveMessage,
            IUser user, InteractiveService service)
            : base(message.Client, message.Model)
        {
            Precondition = interactiveMessage.Precondition;
            Timeout = interactiveMessage.Timeout;

            ReactionCallbacks = interactiveMessage.ReactionCallbacks
                .ToDictionary(k => k.Key, v => new ReactionCallback(v.Value));

            MessageCallbacks = interactiveMessage.MessageCallbacks.ConvertAll(x => new MessageCallback(x)).AsReadOnly();

            User = user;
            TimeoutsAt = DateTimeOffset.UtcNow.AddMilliseconds(interactiveMessage.Timeout);

            _service = service;
            _tokenSource = new Lazy<CancellationTokenSource>();
        }

        public void Stop() => _service.StopInteractiveMessage(this);
    }
}