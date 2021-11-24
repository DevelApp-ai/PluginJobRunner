using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobRunner.Quartz
{
    public class JobSchedule
    {
        public JobSchedule(Type jobType, string cronExpression)
        {
            JobType = jobType;
            CronExpression = cronExpression;
        }

        /// <summary>
        /// The .net type to execute. Could be using the naming used in RuntimePluggableClassFactory
        /// </summary>
        public Type JobType { get; }

        /// <summary>
        /// Cron string expression
        /// </summary>
        public string CronExpression { get; }
    }
}
