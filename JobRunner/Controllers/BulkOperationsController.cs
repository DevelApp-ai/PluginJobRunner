using DevelApp.RuntimePluggableClassFactory.BulkOperations;
using DevelApp.RuntimePluggableClassFactory.Interface;
using DevelApp.RuntimePluggableClassFactory.SemanticVersioning;
using DevelApp.Utility.Model;
using JobExecutorModel;
using JobRunner.Errors;
using Microsoft.AspNetCore.Mvc;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobRunner.Controllers
{
    /// <summary>
    /// Controller demonstrating bulk operations with plugins
    /// Shows how to efficiently execute multiple plugins in batch
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BulkOperationsController : ControllerBase
    {
        private readonly PluginClassFactory<IJobExecutor>? _jobExecutorFactory;
        private readonly TypedPluginClassFactory<IJobExecutor<MergeSortInput, MergeSortOutput>, MergeSortInput, MergeSortOutput>? _typedJobExecutorFactory;

        /// <summary>
        /// Creates a new BulkOperationsController
        /// </summary>
        public BulkOperationsController(
            PluginClassFactory<IJobExecutor>? jobExecutorFactory = null,
            TypedPluginClassFactory<IJobExecutor<MergeSortInput, MergeSortOutput>, MergeSortInput, MergeSortOutput>? typedJobExecutorFactory = null)
        {
            _jobExecutorFactory = jobExecutorFactory;
            _typedJobExecutorFactory = typedJobExecutorFactory;
        }

        /// <summary>
        /// Executes multiple jobs in bulk
        /// Demonstrates: Parallel execution of multiple plugins
        /// </summary>
        [HttpPost("execute-multiple")]
        public async Task<ActionResult<BulkExecutionResult>> ExecuteMultipleJobs([FromBody] BulkJobRequest request)
        {
            if (_jobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Plugin factory not configured"
                });
            }

            if (request?.Jobs == null || request.Jobs.Count == 0)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InvalidJobData,
                    Message = "No jobs specified in request"
                });
            }

            try
            {
                var moduleName = new NamespaceString("JobExecutor");
                var results = new List<IndividualJobResult>();
                var errors = new List<JobError>();

                // Execute each job
                foreach (var jobRequest in request.Jobs)
                {
                    try
                    {
                        var executorName = new IdentifierString(jobRequest.JobExecutor);
                        var executor = _jobExecutorFactory.GetInstance(moduleName, executorName);
                        
                        if (executor == null)
                        {
                            errors.Add(new JobError
                            {
                                Code = JobErrorCode.PluginNotFound,
                                Message = $"Plugin {jobRequest.JobExecutor} not found"
                            });
                            continue;
                        }

                        var result = executor.ExecuteJob(null, jobRequest.JobData);
                        
                        results.Add(new IndividualJobResult
                        {
                            JobExecutor = jobRequest.JobExecutor,
                            JobData = jobRequest.JobData,
                            Success = result.success,
                            ReturnedJobData = result.returnedJobData,
                            Error = result.error
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new JobError
                        {
                            Code = JobErrorCode.PluginExecutionFailed,
                            Message = ex.Message,
                            Exception = ex
                        });
                    }
                }

                return Ok(new BulkExecutionResult
                {
                    TotalJobs = request.Jobs.Count,
                    SuccessfulJobs = results.Count(r => r.Success),
                    FailedJobs = request.Jobs.Count - results.Count(r => r.Success),
                    Results = results,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InternalServerError,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Executes multiple jobs in bulk with parallel execution
        /// Demonstrates: Using PluginBulkOperations for parallel processing
        /// </summary>
        [HttpPost("execute-parallel")]
        public async Task<ActionResult<BulkExecutionResult>> ExecuteParallelJobs([FromBody] BulkJobRequest request)
        {
            if (_jobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Plugin factory not configured"
                });
            }

            if (request?.Jobs == null || request.Jobs.Count == 0)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InvalidJobData,
                    Message = "No jobs specified in request"
                });
            }

            try
            {
                var moduleName = new NamespaceString("JobExecutor");
                
                // Convert request to plugin identifiers
                var pluginIdentifiers = request.Jobs
                    .Select(j => (moduleName, new IdentifierString(j.JobExecutor)))
                    .ToList();

                // Use bulk operations for parallel execution
                var bulkResult = PluginBulkOperations.LoadPlugins(
                    _jobExecutorFactory,
                    pluginIdentifiers,
                    parallel: true);

                var results = new List<IndividualJobResult>();
                var errors = new List<JobError>();

                // Execute each loaded plugin
                foreach (var kvp in bulkResult.Successes)
                {
                    try
                    {
                        var jobRequest = request.Jobs.FirstOrDefault(j => j.JobExecutor == kvp.Key.Name.ToString());
                        if (jobRequest != null)
                        {
                            var result = kvp.Value.ExecuteJob(null, jobRequest.JobData);
                            results.Add(new IndividualJobResult
                            {
                                JobExecutor = jobRequest.JobExecutor,
                                JobData = jobRequest.JobData,
                                Success = result.success,
                                ReturnedJobData = result.returnedJobData,
                                Error = result.error
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new JobError
                        {
                            Code = JobErrorCode.PluginExecutionFailed,
                            Message = ex.Message,
                            Exception = ex
                        });
                    }
                }

                // Add failures from bulk load
                foreach (var kvp in bulkResult.Failures)
                {
                    errors.Add(new JobError
                    {
                        Code = JobErrorCode.PluginLoadFailed,
                        Message = kvp.Value.Message,
                        Exception = kvp.Value.Exception
                    });
                }

                return Ok(new BulkExecutionResult
                {
                    TotalJobs = request.Jobs.Count,
                    SuccessfulJobs = results.Count(r => r.Success),
                    FailedJobs = request.Jobs.Count - results.Count(r => r.Success),
                    Results = results,
                    Errors = errors,
                    ParallelExecution = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InternalServerError,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Executes type-safe jobs in bulk
        /// Demonstrates: Type-safe bulk execution with PluginBulkOperations
        /// </summary>
        [HttpPost("execute-typed-bulk")]
        public async Task<ActionResult<BulkTypedExecutionResult<MergeSortOutput>>> ExecuteTypedBulkJobs([FromBody] BulkTypedJobRequest<MergeSortInput> request)
        {
            if (_typedJobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Typed plugin factory not configured"
                });
            }

            if (request?.Jobs == null || request.Jobs.Count == 0)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InvalidJobData,
                    Message = "No jobs specified in request"
                });
            }

            try
            {
                var moduleName = new NamespaceString("SortArray");
                
                // Convert request to plugin identifiers
                var pluginIdentifiers = request.Jobs
                    .Select(j => (moduleName, new IdentifierString(j.PluginName)))
                    .ToList();

                // Use typed bulk operations for parallel execution
                var bulkResult = await PluginBulkOperations.ExecutePluginsAsync(
                    _typedJobExecutorFactory,
                    pluginIdentifiers,
                    request.Input,
                    parallel: true);

                var results = new List<IndividualTypedResult<MergeSortOutput>>();
                var errors = new List<PluginBulkOperations.PluginExecutionError>();

                // Collect results
                foreach (var kvp in bulkResult.Successes)
                {
                    results.Add(new IndividualTypedResult<MergeSortOutput>
                    {
                        PluginName = kvp.Key.Name.ToString(),
                        Success = kvp.Value.Success,
                        Data = kvp.Value.Data,
                        ErrorMessage = kvp.Value.ErrorMessage,
                        ExecutionDuration = kvp.Value.ExecutionDuration
                    });
                }

                // Collect errors
                foreach (var kvp in bulkResult.Failures)
                {
                    errors.Add(kvp.Value);
                }

                return Ok(new BulkTypedExecutionResult<MergeSortOutput>
                {
                    TotalJobs = request.Jobs.Count,
                    SuccessfulJobs = bulkResult.SuccessCount,
                    FailedJobs = bulkResult.FailureCount,
                    Results = results,
                    Errors = errors,
                    ParallelExecution = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InternalServerError,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all available plugins in bulk
        /// Demonstrates: Bulk plugin discovery
        /// </summary>
        [HttpGet("plugins-all")]
        public async Task<ActionResult<BulkPluginInfo>> GetAllPlugins()
        {
            if (_jobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Plugin factory not configured"
                });
            }

            try
            {
                var allPlugins = _jobExecutorFactory.GetAllInstanceNamesDescriptionsAndVersions();
                
                var pluginInfos = allPlugins.Select(p => new PluginInfo
                {
                    Module = p.ModuleName.ToString(),
                    Name = p.Name.ToString(),
                    Description = p.Description,
                    Versions = p.Versions.Select(v => v.ToString()).ToList()
                }).ToList();

                return Ok(new BulkPluginInfo
                {
                    TotalPlugins = pluginInfos.Count,
                    Plugins = pluginInfos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InternalServerError,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Refreshes all plugins
        /// Demonstrates: Plugin refresh capability
        /// </summary>
        [HttpPost("refresh-plugins")]
        public async Task<IActionResult> RefreshPlugins()
        {
            if (_jobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Plugin factory not configured"
                });
            }

            try
            {
                var result = await _jobExecutorFactory.RefreshPluginsAsync();
                
                return Ok(new
                {
                    Success = result.Success,
                    Count = result.Count,
                    Message = result.Success ? "Plugins refreshed successfully" : "Failed to refresh plugins"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InternalServerError,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
    }

    // ============================================================================
    // Request/Response DTOs
    // ============================================================================

    /// <summary>
    /// Request for bulk job execution
    /// </summary>
    public class BulkJobRequest
    {
        /// <summary>
        /// List of jobs to execute
        /// </summary>
        public List<JobRequest> Jobs { get; set; } = new List<JobRequest>();
    }

    /// <summary>
    /// Individual job request
    /// </summary>
    public class JobRequest
    {
        /// <summary>
        /// Name of the job executor plugin
        /// </summary>
        public string? JobExecutor { get; set; }

        /// <summary>
        /// Job data (JSON string)
        /// </summary>
        public string? JobData { get; set; }
    }

    /// <summary>
    /// Request for bulk type-safe job execution
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    public class BulkTypedJobRequest<TInput>
    {
        /// <summary>
        /// Input data for all jobs
        /// </summary>
        public TInput Input { get; set; } = default!;

        /// <summary>
        /// List of plugin names to execute
        /// </summary>
        public List<PluginRequest> Jobs { get; set; } = new List<PluginRequest>();
    }

    /// <summary>
    /// Plugin request
    /// </summary>
    public class PluginRequest
    {
        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string? PluginName { get; set; }
    }

    /// <summary>
    /// Result of bulk execution
    /// </summary>
    public class BulkExecutionResult
    {
        /// <summary>
        /// Total number of jobs
        /// </summary>
        public int TotalJobs { get; set; }

        /// <summary>
        /// Number of successful jobs
        /// </summary>
        public int SuccessfulJobs { get; set; }

        /// <summary>
        /// Number of failed jobs
        /// </summary>
        public int FailedJobs { get; set; }

        /// <summary>
        /// Individual job results
        /// </summary>
        public List<IndividualJobResult> Results { get; set; } = new List<IndividualJobResult>();

        /// <summary>
        /// Errors that occurred
        /// </summary>
        public List<JobError> Errors { get; set; } = new List<JobError>();

        /// <summary>
        /// Whether execution was parallel
        /// </summary>
        public bool ParallelExecution { get; set; }
    }

    /// <summary>
    /// Individual job result
    /// </summary>
    public class IndividualJobResult
    {
        /// <summary>
        /// Job executor name
        /// </summary>
        public string? JobExecutor { get; set; }

        /// <summary>
        /// Job data
        /// </summary>
        public string? JobData { get; set; }

        /// <summary>
        /// Whether execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Returned job data
        /// </summary>
        public string? ReturnedJobData { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Result of bulk type-safe execution
    /// </summary>
    /// <typeparam name="TOutput">Output type</typeparam>
    public class BulkTypedExecutionResult<TOutput>
    {
        /// <summary>
        /// Total number of jobs
        /// </summary>
        public int TotalJobs { get; set; }

        /// <summary>
        /// Number of successful jobs
        /// </summary>
        public int SuccessfulJobs { get; set; }

        /// <summary>
        /// Number of failed jobs
        /// </summary>
        public int FailedJobs { get; set; }

        /// <summary>
        /// Individual typed results
        /// </summary>
        public List<IndividualTypedResult<TOutput>> Results { get; set; } = new List<IndividualTypedResult<TOutput>>();

        /// <summary>
        /// Errors that occurred
        /// </summary>
        public List<PluginBulkOperations.PluginExecutionError> Errors { get; set; } = new List<PluginBulkOperations.PluginExecutionError>();

        /// <summary>
        /// Whether execution was parallel
        /// </summary>
        public bool ParallelExecution { get; set; }
    }

    /// <summary>
    /// Individual typed result
    /// </summary>
    /// <typeparam name="TOutput">Output type</typeparam>
    public class IndividualTypedResult<TOutput>
    {
        /// <summary>
        /// Plugin name
        /// </summary>
        public string? PluginName { get; set; }

        /// <summary>
        /// Whether execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Output data
        /// </summary>
        public TOutput? Data { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Execution duration
        /// </summary>
        public TimeSpan ExecutionDuration { get; set; }
    }

    /// <summary>
    /// Bulk plugin information
    /// </summary>
    public class BulkPluginInfo
    {
        /// <summary>
        /// Total number of plugins
        /// </summary>
        public int TotalPlugins { get; set; }

        /// <summary>
        /// List of plugins
        /// </summary>
        public List<PluginInfo> Plugins { get; set; } = new List<PluginInfo>();
    }

    /// <summary>
    /// Plugin information
    /// </summary>
    public class PluginInfo
    {
        public string? Module { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? Versions { get; set; }
    }
}
