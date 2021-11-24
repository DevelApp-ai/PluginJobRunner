using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace JobExecutorModel
{
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
        public string JobExecutor { get; set; }

        /// <summary>
        /// Data of the job. This might be updated depending on job execution
        /// </summary>
        public string JobData { get; set; }

        /// <summary>
        /// Messages about the job execution
        /// </summary>
        public string JobExecutionMessage { get; set; }
    }
}
