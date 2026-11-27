using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JobRunner.Telemetry
{
    /// <summary>
    /// OpenTelemetry emission point for the job substrate (decision
    /// DevelApp-Repo-Global #89, PJR-3). Consumers register an
    /// OpenTelemetry .NET SDK tracer with the source name below (or simply
    /// listen via the DiagnosticListener); spans are started per job
    /// execution with the correlation id as a trace attribute, so a job
    /// emitted by one host and executed by another links into one trace.
    /// No OpenTelemetry package dependency: System.Diagnostics.ActivitySource
    /// is the OpenTelemetry-compatible emission API in the BCL.
    /// </summary>
    public static class JobTelemetry
    {
        public const string ActivitySourceName = "DevelApp.PluginJobRunner";

        public const string ActivityNameJobExecution = "DevelApp.PluginJobRunner.JobExecution";

        // Tag keys (stable wire contract)
        public const string TagJobId = "pjr.job.job_id";
        public const string TagCorrelationId = "pjr.job.correlation_id";
        public const string TagJobExecutor = "pjr.job.executor";
        public const string TagTriggerId = "pjr.job.trigger_id";
        public const string TagEmittedBy = "pjr.job.emitted_by";

        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);

        /// <summary>
        /// Starts a job-execution activity tagged with the job identity.
        /// Returns null when no listener is attached (standard Activity
        /// semantics), so callers must null-check before disposing.
        /// </summary>
        public static Activity StartJobActivity(
            string jobId,
            string correlationId,
            string jobExecutorName,
            string triggerId = null,
            string emittedBy = null)
        {
            var activity = ActivitySource.StartActivity(ActivityNameJobExecution, ActivityKind.Internal);
            if (activity != null)
            {
                activity.SetTag(TagJobId, jobId ?? string.Empty);
                activity.SetTag(TagCorrelationId, correlationId ?? string.Empty);
                activity.SetTag(TagJobExecutor, jobExecutorName ?? string.Empty);
                if (!string.IsNullOrEmpty(triggerId))
                {
                    activity.SetTag(TagTriggerId, triggerId);
                }
                if (!string.IsNullOrEmpty(emittedBy))
                {
                    activity.SetTag(TagEmittedBy, emittedBy);
                }
            }
            return activity;
        }
    }
}
