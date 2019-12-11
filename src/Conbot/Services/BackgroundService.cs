using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Conbot.Services
{
    public abstract class BackgroundService : IHostedService, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _task;

        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            _task = ExecuteAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_task != null)
                return;

            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch
            {
                await _task;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}