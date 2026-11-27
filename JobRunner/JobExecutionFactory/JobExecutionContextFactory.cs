using JobExecutorModel;
using PluginInterface;
using System;
using System.Globalization;

namespace JobRunner.JobExecutionFactory
{
    /// <summary>
    /// Builds IJobExecutionContext instances for the JobRunner dispatch sites
    /// (decision DevelApp-Repo-Global #89, PJR-1). Replaces the previous
    /// "pass null context" dispatch so plugins receive the blessed contract.
    /// </summary>
    public static class JobExecutionContextFactory
    {
        /// <summary>
        /// New correlation id for jobs submitted without one
        /// </summary>
        public static string NewCorrelationId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Context for a persisted Job entity
        /// </summary>
        public static JobExecutionContext CreateForJob(Job job)
        {
            return new JobExecutionContext(
                jobId: job.JobId.ToString(CultureInfo.InvariantCulture),
                correlationId: string.IsNullOrEmpty(job.CorrelationId) ? NewCorrelationId() : job.CorrelationId,
                jobExecutorName: job.JobExecutor ?? string.Empty,
                emittedBy: job.EmittedBy,
                triggerId: job.TriggerId,
                triggerExpression: job.TriggerExpression,
                enqueuedAtUtc: new DateTimeOffset(job.Enqueued, TimeSpan.Zero));
        }

        /// <summary>
        /// Context for a bulk JobRequest (JobRunner.Controllers.JobRequest)
        /// </summary>
        public static JobExecutionContext CreateForRequest(JobRunner.Controllers.JobRequest request)
        {
            return new JobExecutionContext(
                jobId: "unsaved",
                correlationId: string.IsNullOrEmpty(request.CorrelationId) ? NewCorrelationId() : request.CorrelationId,
                jobExecutorName: request.JobExecutor ?? string.Empty,
                emittedBy: request.EmittedBy,
                triggerId: request.TriggerId);
        }

        /// <summary>
        /// Context for a request that has not been persisted yet (direct execute endpoints)
        /// </summary>
        public static JobExecutionContext CreateForRequest(PostJobRequest request)
        {
            return new JobExecutionContext(
                jobId: "unsaved",
                correlationId: string.IsNullOrEmpty(request.CorrelationId) ? NewCorrelationId() : request.CorrelationId,
                jobExecutorName: request.JobExecutor ?? string.Empty,
                emittedBy: request.EmittedBy,
                triggerId: request.TriggerId);
        }
    }
}
