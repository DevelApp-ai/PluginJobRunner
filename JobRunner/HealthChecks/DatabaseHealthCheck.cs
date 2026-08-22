using JobExecutorModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JobRunner.HealthChecks
{
    /// <summary>
    /// Health check for database connectivity
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly JobDbContext _dbContext;
        
        public DatabaseHealthCheck(JobDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Test if database is reachable
                if (!_dbContext.Database.CanConnect())
                {
                    return HealthCheckResult.Unhealthy("Database connection failed");
                }
                
                // Test a simple query to ensure database is operational
                var canQuery = await _dbContext.Job.AnyAsync(cancellationToken);
                
                return HealthCheckResult.Healthy("Database is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed", ex);
            }
        }
    }
}
