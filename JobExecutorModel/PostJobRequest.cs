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
        public string JobExecutor { get; set; }

        /// <summary>
        /// Data of the job. This might be updated depending on job execution
        /// </summary>
        public string JobData { get; set; }
    }
}
