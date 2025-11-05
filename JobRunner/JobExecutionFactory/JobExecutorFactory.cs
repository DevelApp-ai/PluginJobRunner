using DevelApp.RuntimePluggableClassFactory;
using DevelApp.RuntimePluggableClassFactory.FilePlugin;
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
    /// Enhanced with error handling and security validation from RuntimePluggableClassFactory v2.1.0
    /// Addresses README challenges:
    /// - "All dlls in the plugin library and their dependencies are loaded" - Now uses security validation
    /// - "Failing JobExecutor should not be killing the whole application" - Now has graceful error handling
    /// </summary>
    public class JobExecutorFactory : IDisposable
    {
        private PluginClassFactory<IJobExecutor> _jobExecutorFactory;
        private FilePluginLoader<IJobExecutor> _pluginLoader;
        private bool _disposed = false;

        public JobExecutorFactory(Uri pluginPathUri)
        {
            try
            {
                // Create plugin loader with default security validator (filters unsafe plugins)
                _pluginLoader = new FilePluginLoader<IJobExecutor>(pluginPathUri);
                
                // Subscribe to error events for graceful degradation
                _pluginLoader.PluginLoadingFailed += OnPluginLoadingFailed;
                _pluginLoader.SecurityValidationFailed += OnSecurityValidationFailed;
                
                _jobExecutorFactory = new PluginClassFactory<IJobExecutor>(_pluginLoader);
                
                // Subscribe to factory error events (note: event name is PluginInstantiationFailed not PluginInstantiationError)
                _jobExecutorFactory.PluginInstantiationFailed += OnPluginInstantiationFailed;
                
                Log.Information("JobExecutorFactory initialized with enhanced error handling and security validation");
            }
            catch (Exception ex)
            {
                throw new JobRunnerException($"Error occured when loading JobExecutors from {pluginPathUri.LocalPath}", ex);
            }
        }

        /// <summary>
        /// Handles plugin loading failures - logs error without crashing the application
        /// </summary>
        private void OnPluginLoadingFailed(object sender, PluginLoadingErrorEventArgs e)
        {
            Log.Warning(e.Exception, 
                "Failed to load plugin from {FileName} in {PluginPath}. Plugin will be skipped. Error: {ErrorMessage}",
                e.FileName, e.PluginPath, e.Exception.Message);
        }

        /// <summary>
        /// Handles security validation failures - logs security issues without crashing
        /// </summary>
        private void OnSecurityValidationFailed(object sender, PluginSecurityValidationFailedEventArgs e)
        {
            if (!e.ValidationResult.IsValid)
            {
                var issues = e.ValidationResult.Issues.Select(i => $"{i.Severity}: {i.Description}");
                Log.Warning(
                    "Security validation failed for plugin {FileName} in {PluginPath}. Risk Level: {RiskLevel}. Issues: {Issues}",
                    e.FileName, e.PluginPath, e.ValidationResult.RiskLevel, string.Join(", ", issues));
            }
            else if (e.ValidationResult.Warnings.Any())
            {
                var warnings = e.ValidationResult.Warnings.Select(w => w.Description);
                Log.Information(
                    "Security validation warnings for plugin {FileName}: {Warnings}",
                    e.FileName, string.Join(", ", warnings));
            }
        }

        /// <summary>
        /// Handles plugin instantiation errors - logs error without crashing
        /// </summary>
        private void OnPluginInstantiationFailed(object sender, PluginInstantiationErrorEventArgs e)
        {
            Log.Error(e.Exception,
                "Failed to instantiate plugin {ModuleName}.{PluginName} version {Version}. Error: {ErrorMessage}",
                e.ModuleName, e.PluginName, e.Version, e.Exception.Message);
        }

        /// <summary>
        /// Returns the JobExecutor requested and null if it does not exist
        /// Enhanced with better error handling that doesn't crash the application
        /// </summary>
        /// <param name="jobRunnerFullName"></param>
        /// <returns></returns>
        public IJobExecutor GetJobExecutor(string jobRunnerFullName)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JobExecutorFactory));
            }

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
                    
                    if (jobExecutor != null)
                    {
                        Log.Debug("Successfully retrieved JobExecutor instance for {FullName}", jobRunnerFullName);
                    }
                    else
                    {
                        Log.Warning("JobExecutor {FullName} not found in loaded plugins", jobRunnerFullName);
                    }
                }
                catch (Exception ex)
                {
                    // Graceful error handling - log but don't crash the application
                    Log.Error(ex, $"Could not get an instance of {moduleName}.{jobName}. Application continues.");
                }
            }
            else
            {
                Log.Warning("Invalid JobExecutor name format: {FullName}. Expected format: Namespace.ClassName", jobRunnerFullName);
            }
            return jobExecutor;
        }

        /// <summary>
        /// Disposes of resources and unsubscribes from events
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events
                    if (_pluginLoader != null)
                    {
                        _pluginLoader.PluginLoadingFailed -= OnPluginLoadingFailed;
                        _pluginLoader.SecurityValidationFailed -= OnSecurityValidationFailed;
                    }

                    if (_jobExecutorFactory != null)
                    {
                        _jobExecutorFactory.PluginInstantiationFailed -= OnPluginInstantiationFailed;
                    }

                    // Dispose plugin loader (includes unloading plugins)
                    (_pluginLoader as IDisposable)?.Dispose();
                    
                    Log.Information("JobExecutorFactory disposed and plugins unloaded");
                }

                _disposed = true;
            }
        }
    }
}
