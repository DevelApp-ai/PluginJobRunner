using DevelApp.RuntimePluggableClassFactory;
using DevelApp.Utility.Model;
using JobRunner.Exceptions;
using PluginInterface;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobRunner.JobExecutionFactory
{
    /// <summary>
    /// Holds the factory for JobExecutor coming from the plugin directory
    /// </summary>
    public class JobExecutorFactory
    {
        private PluginClassFactory<IJobExecutor> _jobExecutorFactory;

        public JobExecutorFactory(Uri pluginPathUri)
        {
            //Support only the latest version of the plugin
            int retainOldVersions = 0;
            try
            {
                _jobExecutorFactory = new PluginClassFactory<IJobExecutor>(retainOldVersions);
                _jobExecutorFactory.LoadFromDirectory(pluginPathUri);
            }
            catch (Exception ex)
            {
                throw new JobRunnerException($"Error occured when loading JobExecutors from {pluginPathUri.LocalPath}", ex);
            }
        }

        /// <summary>
        /// Returns the JobExecutor requested and null if it does not exist
        /// </summary>
        /// <param name="jobRunnerFullName"></param>
        /// <returns></returns>
        public IJobExecutor GetJobExecutor(string jobRunnerFullName)
        {
            string[] jobRunnerNamePart = jobRunnerFullName.Split('.');
            IJobExecutor jobExecutor = null;
            if (jobRunnerNamePart.Length >= 2)
            {
                NamespaceString moduleName = null;
                IdentifierString jobName = null;

                try
                {
                    jobName = jobRunnerNamePart[jobRunnerNamePart.Length - 1];
                    moduleName = jobRunnerFullName.Substring(0, jobRunnerFullName.LastIndexOf('.'));
                    jobExecutor = _jobExecutorFactory.GetInstance(moduleName, jobName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Could not get an instance of {moduleName}.{jobName}");
                }
            }
            return jobExecutor;
        }
    }
}
