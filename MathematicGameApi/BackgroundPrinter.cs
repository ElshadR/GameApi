using MathematicGameApi.Infrastructure.Services.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MathematicGameApi
{
    public class BackgroundPrinter : IHostedService//, IDisposable
    {
        private Timer timer;
        private readonly ICoreService _coreService;
        public BackgroundPrinter(ICoreService coreService)
        {
            _coreService = coreService;
        }
        public void Dispose()
        {
            timer?.Dispose();
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(300));
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            _coreService.IncreaseUserLifeForTimer();
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

    }
}
