using DevelApp.RuntimePluggableClassFactory;
using DevelApp.RuntimePluggableClassFactory.FilePlugin;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PluginInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JobRunner.HealthChecks
{
    /// <summary>
    /// Health check for plugin loader
    /// Verifies that plugins can be loaded successfully
    /// </summary>
    public class PluginLoaderHealthCheck : IHealthCheck
    {
        private readonly IPluginLoader<IJobExecutor>? _pluginLoader;
        private readonly PluginClassFactory<IJobExecutor>? _pluginFactory;

        /// <summary>
        /// Creates a new plugin loader health check
        /// </summary>
        /// <param name="pluginLoader">Plugin loader to check</param>
        /// <param name="pluginFactory">Plugin factory to check</param>
        public PluginLoaderHealthCheck(
            IPluginLoader<IJobExecutor>? pluginLoader = null,
            PluginClassFactory<IJobExecutor>? pluginFactory = null)
        {
            _pluginLoader = pluginLoader;
            _pluginFactory = pluginFactory;
        }

        /// <summary>
        /// Name of the health check
        /// </summary>
        public string Name => "Plugin Loader";

        /// <summary>
        /// Checks the health of the plugin loader
        /// </summary>
        /// <param name="context">Health check context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if plugin loader is available
                if (_pluginLoader == null)
                {
                    return HealthCheckResult.Degraded("Plugin loader not configured");
                }

                // Try to list all possible plugins
                var plugins = await _pluginLoader.ListAllPossiblePluginsAsync();
                
                // Check if we have any plugins loaded
                var pluginCount = 0;
                foreach (var plugin in plugins)
                {
                    pluginCount++;
                }

                // If we have plugins, check the factory
                if (_pluginFactory != null)
                {
                    var allInstances = _pluginFactory.GetAllInstanceNamesDescriptionsAndVersions();
                    var factoryCount = 0;
                    foreach (var instance in allInstances)
                    {
                        factoryCount++;
                    }

                    if (factoryCount == 0)
                    {
                        return HealthCheckResult.Degraded(
                            "Plugin loader is working but no plugins are loaded in the factory");
                    }

                    return HealthCheckResult.Healthy(
                        $"Plugin loader is healthy with {pluginCount} plugins available, {factoryCount} loaded in factory");
                }

                return HealthCheckResult.Healthy(
                    $"Plugin loader is healthy with {pluginCount} plugins available");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Plugin loader health check failed",
                    ex);
            }
        }
    }
}
