using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;
using Quartz.Impl;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JobRunner.HealthChecks
{
    /// <summary>
    /// Health check for Quartz scheduler
    /// </summary>
    public class QuartzHealthCheck : IHealthCheck
    {
        private readonly ISchedulerFactory _schedulerFactory;
        
        public QuartzHealthCheck(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
                
                if (scheduler == null)
                {
                    return HealthCheckResult.Unhealthy("Scheduler factory returned null");
                }
                
                // IsStarted is a property on IScheduler, not a method
                var isRunning = scheduler.IsStarted;
                
                if (!isRunning)
                {
                    return HealthCheckResult.Unhealthy("Quartz scheduler is not running");
                }
                
                return HealthCheckResult.Healthy("Quartz scheduler is healthy and running");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Quartz health check failed", ex);
            }
        }
    }
}
