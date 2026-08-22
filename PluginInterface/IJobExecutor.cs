using DevelApp.RuntimePluggableClassFactory.Interface;
using System;

namespace PluginInterface
{
    public interface IJobExecutor:IPluginClass
    {
        /// <summary>
        /// Executes the job and returns a status. Jobdata should be in json.
        /// </summary>
        /// <param name="jobExecutionContext"></param>
        /// <param name="jobData"></param>
        /// <returns></returns>
        // Consider using strongly-typed DTOs instead of string for better type safety
        (bool success, string returnedJobData, string error) ExecuteJob(IJobExecutionContext jobExecutionContext, string jobData);
    }
}
