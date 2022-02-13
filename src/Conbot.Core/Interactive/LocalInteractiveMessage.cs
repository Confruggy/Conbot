using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Disqord;

namespace Conbot.Interactive;

public class LocalInteractiveMessage : LocalMessage
{
    public Func<IUser, Task<bool>>? Precondition { get; set; }
    public int Timeout { get; set; } = 600000;
    public Dictionary<LocalEmoji, LocalReactionCallback> ReactionCallbacks { get; set; }
    public List<LocalMessageCallback> MessageCallbacks { get; set; }

    public LocalInteractiveMessage()
    {
        MessageCallbacks = new List<LocalMessageCallback>();
        ReactionCallbacks = new Dictionary<LocalEmoji, LocalReactionCallback>();
    }
}