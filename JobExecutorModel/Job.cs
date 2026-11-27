using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JobExecutorModel
{
    [Index(nameof(CorrelationId))]
    public class Job
    {
        /// <summary>
        /// Unique id for Job
        /// </summary>
        [Key]
        public int JobId { get; set; }

        /// <summary>
        /// The time the job was encueued for processing
        /// </summary>
        public DateTime Enqueued { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The time the job took to execute
        /// </summary>
        public TimeSpan JobExecutionDuration { get; set; }

        /// <summary>
        /// The status of the job
        /// </summary>
        public JobStatus JobStatus { get; set; }

        /// <summary>
        /// Job executor is the task doing the job on the data
        /// </summary>
        public string? JobExecutor { get; set; }

        /// <summary>
        /// Data of the job. This might be updated depending on job execution
        /// </summary>
        public string? JobData { get; set; }

        /// <summary>
        /// Messages about the job execution
        /// </summary>
        public string? JobExecutionMessage { get; set; }

        /// <summary>
        /// Correlation id shared across hosts (decision DevelApp-Repo-Global #89,
        /// PJR-2). A job emitted by one host and executed by another carries
        /// the same value; also the OpenTelemetry trace attribute.
        /// </summary>
        [MaxLength(128)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Host that emitted the job (e.g. "LaunchMobile-Server"). Null for
        /// locally created jobs.
        /// </summary>
        [MaxLength(256)]
        public string? EmittedBy { get; set; }

        /// <summary>
        /// Scheduler trigger identity (Quartz trigger key). Null for manually
        /// submitted jobs.
        /// </summary>
        [MaxLength(450)]
        public string? TriggerId { get; set; }

        /// <summary>
        /// Scheduling expression (cron or interval) for trigger-driven jobs.
        /// Null for manually submitted jobs.
        /// </summary>
        [MaxLength(256)]
        public string? TriggerExpression { get; set; }

        /// <summary>
        /// Next scheduled fire time (UTC) for trigger-driven jobs.
        /// </summary>
        public DateTime? NextFireAtUtc { get; set; }

        /// <summary>
        /// Time (UTC) of the last trigger outcome; paired with
        /// JobExecutionMessage/JobExecutionDuration this is the "last outcome"
        /// of the monitoring contract.
        /// </summary>
        public DateTime? LastOutcomeAtUtc { get; set; }
    }
}
