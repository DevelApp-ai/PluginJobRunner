using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobExecutorModel
{
    public enum JobStatus
    {
        /// <summary>
        /// Job has not run yet
        /// </summary>
        Pending = 0,
        /// <summary>
        /// Job is currently running
        /// </summary>
        Running = 1,
        /// <summary>
        /// Job has run successfully
        /// </summary>
        Success = 2,
        /// <summary>
        /// Job has run but failed
        /// </summary>
        Failed = 3
    }
}
