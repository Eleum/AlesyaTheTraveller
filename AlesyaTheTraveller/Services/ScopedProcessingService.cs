using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Services
{
    internal interface IScopedProcessingService
    {
        void DoWork();
    }

    public class ScopedProcessingService : IScopedProcessingService
    {
        private readonly ILogger _logger;

        public ScopedProcessingService(ILogger<ScopedProcessingService> logger)
        {
            _logger = logger;
        }

        public void DoWork()
        {

        }
    }

    public class ConsumeScopedServiceHostedService : IHostedService
    {
        private readonly ILogger _logger;

        public IServiceProvider Services { get; }

        public ConsumeScopedServiceHostedService(IServiceProvider services, 
            ILogger<ConsumeScopedServiceHostedService> logger)
        {
            Services = services;
            _logger = logger;
        }

        private void DoWork()
        {
            _logger.LogInformation("Consume Scoped Service Hosted Service is working");

            using (var scope = Services.CreateScope())
            {
                var scopedProcessingService = scope.ServiceProvider.GetRequiredService<IScopedProcessingService>();
                scopedProcessingService.DoWork();
            }
        }

        public Task StartAsync(CancellationToken token)
        {
            _logger.LogInformation("Consume Scoped Service Hosted Service is starting");
            DoWork();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is stopping");

            return Task.CompletedTask;
        }
    }
}
