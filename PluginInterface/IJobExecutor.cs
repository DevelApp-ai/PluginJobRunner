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
        // TODO replace string with more specific JsonData to avoid passing error data to jobExecutor
        (bool success, string returnedJobData, string error) ExecuteJob(IJobExecutionContext jobExecutionContext, string jobData);
    }
}
