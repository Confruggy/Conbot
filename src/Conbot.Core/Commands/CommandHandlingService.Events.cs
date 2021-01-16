using Microsoft.Extensions.Hosting;

using Qommon.Events;

namespace Conbot.Commands
{
    public partial class CommandHandlingService : IHostedService
    {
        private readonly AsynchronousEvent<CommandErrorMessageSentEventArgs> _errorMessageSent = new();

        public event AsynchronousEventHandler<CommandErrorMessageSentEventArgs> CommandErrorMessageSent
        {
            add => _errorMessageSent.Hook(value);
            remove => _errorMessageSent.Unhook(value);
        }
    }
}
