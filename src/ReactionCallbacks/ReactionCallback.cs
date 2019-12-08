using System;
using System.Threading.Tasks;
using Discord;

namespace Conbot.ReactionCallbacks
{
    public class ReactionCallback
    {
        public bool ResumeAfterExecution { get; set; }

        public Func<IUser, Task> Function { get; set; }
    }
}