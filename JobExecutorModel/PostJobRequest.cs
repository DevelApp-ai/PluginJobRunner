using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobExecutorModel
{
    public class PostJobRequest
    {
        /// <summary>
        /// Job executor is the task doing the job on the data
        /// </summary>
        public string? JobExecutor { get; set; }

        /// <summary>
        /// Data of the job. This might be updated depending on job execution
        /// </summary>
        public string? JobData { get; set; }

        /// <summary>
        /// Optional correlation id for cross-host tracing (decision #89).
        /// When omitted the JobRunner assigns one.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Optional name of the emitting host (for portable job records)
        /// </summary>
        public string? EmittedBy { get; set; }

        /// <summary>
        /// Optional scheduler trigger identity when the job is trigger-driven
        /// </summary>
        public string? TriggerId { get; set; }
    }
}
