using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading.Tasks;

namespace JobRunner.Quartz
{
    [DisallowConcurrentExecution]
    public class HelloWorldJob : IJob
    {
        private readonly ILogger<HelloWorldJob> _logger;
        public HelloWorldJob(ILogger<HelloWorldJob> logger)
        {
            _logger = logger;
        }

        //Scoped services: https://andrewlock.net/creating-a-quartz-net-hosted-service-with-asp-net-core/#using-scoped-services-in-jobs

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Hello world!");
            return Task.CompletedTask;
        }
    }
}
