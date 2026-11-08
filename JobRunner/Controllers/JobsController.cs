using DevelApp.RuntimePluggableClassFactory;
using DevelApp.RuntimePluggableClassFactory.Async;
using DevelApp.RuntimePluggableClassFactory.Caching;
using DevelApp.RuntimePluggableClassFactory.Resilience;
using DevelApp.RuntimePluggableClassFactory.SemanticVersioning;
using DevelApp.Utility.Model;
using JobExecutorModel;
using JobRunner.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobRunner.Controllers
{
    /// <summary>
    /// API controller for managing and executing jobs
    /// Demonstrates various RuntimePluggableClassFactory capabilities:
    /// - Basic plugin loading and execution
    /// - Async plugin loading
    /// - Cached plugin instances
    /// - Resilient execution with circuit breaker and retry
    /// - Type-safe plugin execution
    /// - Bulk operations
    /// - Version range queries
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly JobDbContext _context;
        private readonly PluginClassFactory<IJobExecutor>? _jobExecutorFactory;
        private readonly AsyncPluginClassFactory<IJobExecutor>? _asyncJobExecutorFactory;
        private readonly CachedPluginClassFactory<IJobExecutor>? _cachedJobExecutorFactory;
        private readonly ResilientPluginClassFactory<IJobExecutor>? _resilientJobExecutorFactory;
        private readonly TypedPluginClassFactory<IJobExecutor<MergeSortInput, MergeSortOutput>, MergeSortInput, MergeSortOutput>? _typedJobExecutorFactory;

        /// <summary>
        /// Creates a new JobsController
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="jobExecutorFactory">Basic plugin factory</param>
        /// <param name="asyncJobExecutorFactory">Async plugin factory</param>
        /// <param name="cachedJobExecutorFactory">Cached plugin factory</param>
        /// <param name="resilientJobExecutorFactory">Resilient plugin factory</param>
        /// <param name="typedJobExecutorFactory">Type-safe plugin factory</param>
        public JobsController(
            JobDbContext context,
            PluginClassFactory<IJobExecutor>? jobExecutorFactory = null,
            AsyncPluginClassFactory<IJobExecutor>? asyncJobExecutorFactory = null,
            CachedPluginClassFactory<IJobExecutor>? cachedJobExecutorFactory = null,
            ResilientPluginClassFactory<IJobExecutor>? resilientJobExecutorFactory = null,
            TypedPluginClassFactory<IJobExecutor<MergeSortInput, MergeSortOutput>, MergeSortInput, MergeSortOutput>? typedJobExecutorFactory = null)
        {
            _context = context;
            _jobExecutorFactory = jobExecutorFactory;
            _asyncJobExecutorFactory = asyncJobExecutorFactory;
            _cachedJobExecutorFactory = cachedJobExecutorFactory;
            _resilientJobExecutorFactory = resilientJobExecutorFactory;
            _typedJobExecutorFactory = typedJobExecutorFactory;
        }

        // ========================================================================
        // Basic CRUD Operations
        // ========================================================================

        // GET: api/Jobs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetJob()
        {
            return await _context.Job.ToListAsync();
        }

        // GET: api/Jobs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJob([FromQuery, BindRequired] int id)
        {
            var job = await _context.Job.FindAsync(id);

            if (job == null)
            {
                return NotFound();
            }

            return job;
        }

        // PUT: api/Jobs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutJob([FromQuery, BindRequired] int id, [FromBody] Job job)
        {
            if (id != job.JobId)
            {
                return BadRequest();
            }

            _context.Entry(job).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Jobs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob([FromQuery, BindRequired] int id)
        {
            var job = await _context.Job.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            _context.Job.Remove(job);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool JobExists(int id)
        {
            return _context.Job.Any(e => e.JobId == id);
        }

        // ========================================================================
        // Job Execution Endpoints - Demonstrating Various Factory Capabilities
        // ========================================================================

        /// <summary>
        /// Creates and executes a job using basic plugin factory
        /// Demonstrates: Basic plugin loading and execution
        /// </summary>
        [HttpPost("execute")]
        public async Task<ActionResult<JobResponse>> PostJob([FromBody] PostJobRequest postJobRequest)
        {
            // Validate job executor is available using basic factory
            if (_jobExecutorFactory != null)
            {
                var executorName = new IdentifierString(postJobRequest.JobExecutor);
                var moduleName = new NamespaceString("JobExecutor");
                
                if (!_jobExecutorFactory.IsPluginAvailable(moduleName, executorName))
                {
                    return BadRequest(new JobError
                    {
                        Code = JobErrorCode.PluginNotFound,
                        Category = JobErrorCategory.Plugin,
                        Message = $"Job executor {postJobRequest.JobExecutor} is not available"
                    });
                }
            }

            // Create job entity
            Job job = new Job();
            job.JobExecutor = postJobRequest.JobExecutor;
            job.JobData = postJobRequest.JobData;

            _context.Job.Add(job);
            await _context.SaveChangesAsync();

            // Try to execute the job
            JobResponse jobResponse = new JobResponse();
            jobResponse.JobId = job.JobId;
            jobResponse.Success = false;

            if (_jobExecutorFactory != null)
            {
                try
                {
                    var executorName = new IdentifierString(postJobRequest.JobExecutor);
                    var moduleName = new NamespaceString("JobExecutor");
                    
                    var executor = _jobExecutorFactory.GetInstance(moduleName, executorName);
                    if (executor != null)
                    {
                        var result = executor.ExecuteJob(null, postJobRequest.JobData);
                        jobResponse.Success = result.success;
                        jobResponse.JobExecutionMessage = result.success ? "Job executed successfully" : result.error;
                        jobResponse.ReturnedJobData = result.returnedJobData;
                    }
                }
                catch (Exception ex)
                {
                    jobResponse.JobExecutionMessage = ex.Message;
                }
            }

            job.JobExecutionMessage = jobResponse.JobExecutionMessage;
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetJob", new { id = job.JobId }, jobResponse);
        }

        /// <summary>
        /// Executes a job asynchronously using async plugin factory
        /// Demonstrates: Async plugin loading and non-blocking execution
        /// </summary>
        [HttpPost("execute-async")]
        public async Task<ActionResult<JobResponse>> PostJobAsync([FromBody] PostJobRequest postJobRequest)
        {
            if (_asyncJobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Async plugin factory not configured"
                });
            }

            try
            {
                var executorName = new IdentifierString(postJobRequest.JobExecutor);
                var moduleName = new NamespaceString("JobExecutor");
                
                // Use async factory for non-blocking plugin loading
                var executor = await _asyncJobExecutorFactory.GetInstanceAsync(moduleName, executorName);
                
                if (executor == null)
                {
                    return BadRequest(new JobError
                    {
                        Code = JobErrorCode.PluginNotFound,
                        Message = $"Job executor {postJobRequest.JobExecutor} not found"
                    });
                }

                var result = executor.ExecuteJob(null, postJobRequest.JobData);
                
                var jobResponse = new JobResponse
                {
                    Success = result.success,
                    JobExecutionMessage = result.success ? "Job executed successfully" : result.error,
                    ReturnedJobData = result.returnedJobData
                };

                return Ok(jobResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.PluginExecutionFailed,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Executes a job using cached plugin factory
        /// Demonstrates: Instance caching for improved performance
        /// </summary>
        [HttpPost("execute-cached")]
        public async Task<ActionResult<JobResponse>> PostJobCached([FromBody] PostJobRequest postJobRequest)
        {
            if (_cachedJobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Cached plugin factory not configured"
                });
            }

            try
            {
                var executorName = new IdentifierString(postJobRequest.JobExecutor);
                var moduleName = new NamespaceString("JobExecutor");
                
                // Use cached factory for potentially faster execution (cached instances)
                var executor = _cachedJobExecutorFactory.GetInstance(moduleName, executorName);
                
                if (executor == null)
                {
                    return BadRequest(new JobError
                    {
                        Code = JobErrorCode.PluginNotFound,
                        Message = $"Job executor {postJobRequest.JobExecutor} not found"
                    });
                }

                var result = executor.ExecuteJob(null, postJobRequest.JobData);
                
                // Get cache statistics
                var stats = _cachedJobExecutorFactory.GetCacheStatistics();
                
                var jobResponse = new JobResponse
                {
                    Success = result.success,
                    JobExecutionMessage = result.success ? "Job executed successfully" : result.error,
                    ReturnedJobData = result.returnedJobData,
                    CacheHit = true // Indicate this used caching
                };

                return Ok(jobResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.PluginExecutionFailed,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Executes a job using resilient plugin factory
        /// Demonstrates: Circuit breaker and retry policies for fault tolerance
        /// </summary>
        [HttpPost("execute-resilient")]
        public async Task<ActionResult<JobResponse>> PostJobResilient([FromBody] PostJobRequest postJobRequest)
        {
            if (_resilientJobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Resilient plugin factory not configured"
                });
            }

            // Check circuit breaker state
            if (_resilientJobExecutorFactory.IsCircuitOpen)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new JobError
                {
                    Code = JobErrorCode.ServiceUnavailable,
                    Category = JobErrorCategory.System,
                    Message = "Circuit breaker is open - plugin execution temporarily unavailable",
                    IsRetryable = true,
                    IsTransient = true
                });
            }

            try
            {
                var executorName = new IdentifierString(postJobRequest.JobExecutor);
                var moduleName = new NamespaceString("JobExecutor");
                
                // Use resilient factory with circuit breaker and retry protection
                var executor = await _resilientJobExecutorFactory.GetInstanceAsync(moduleName, executorName);
                
                if (executor == null)
                {
                    return BadRequest(new JobError
                    {
                        Code = JobErrorCode.PluginNotFound,
                        Message = $"Job executor {postJobRequest.JobExecutor} not found"
                    });
                }

                var result = executor.ExecuteJob(null, postJobRequest.JobData);
                
                var jobResponse = new JobResponse
                {
                    Success = result.success,
                    JobExecutionMessage = result.success ? "Job executed successfully with resilience" : result.error,
                    ReturnedJobData = result.returnedJobData,
                    ResilientExecution = true
                };

                return Ok(jobResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.PluginExecutionFailed,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Executes a type-safe job using typed plugin factory
        /// Demonstrates: Type-safe plugin execution with strongly-typed input/output
        /// </summary>
        [HttpPost("execute-typed")]
        public async Task<ActionResult<JobExecutionResult<MergeSortOutput>>> PostJobTyped([FromBody] MergeSortInput input)
        {
            if (_typedJobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Typed plugin factory not configured"
                });
            }

            try
            {
                var moduleName = new NamespaceString("SortArray");
                var pluginName = new IdentifierString("MergeSortGeneric");
                
                // Use typed factory for type-safe execution
                var result = await _typedJobExecutorFactory.ExecutePluginAsync(
                    moduleName,
                    pluginName,
                    input);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.PluginExecutionFailed,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets information about available plugins
        /// Demonstrates: Plugin discovery and metadata retrieval
        /// </summary>
        [HttpGet("plugins")]
        public async Task<ActionResult<IEnumerable<PluginInfo>>> GetAvailablePlugins()
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
                var plugins = _jobExecutorFactory.GetAllInstanceNamesDescriptionsAndVersions();
                
                var pluginInfos = plugins.Select(p => new PluginInfo
                {
                    Module = p.ModuleName.ToString(),
                    Name = p.Name.ToString(),
                    Description = p.Description,
                    Versions = p.Versions.Select(v => v.ToString()).ToList(),
                    NewestVersion = p.Versions.OrderByDescending().FirstOrDefault()?.ToString()
                }).ToList();

                return Ok(pluginInfos);
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
        /// Gets plugins by version range
        /// Demonstrates: Version range querying
        /// </summary>
        [HttpGet("plugins/by-version")]
        public async Task<ActionResult<IEnumerable<PluginInfo>>> GetPluginsByVersionRange([FromQuery] string versionRange)
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
                var range = VersionRange.Parse(versionRange);
                var plugins = await _jobExecutorFactory.GetPluginsByVersionRangeAsync(range);
                
                var pluginInfos = plugins.Select(p => new PluginInfo
                {
                    Module = p.moduleName.ToString(),
                    Name = p.pluginName.ToString(),
                    Description = p.Description,
                    Version = p.version.ToString()
                }).ToList();

                return Ok(pluginInfos);
            }
            catch (Exception ex)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.InvalidInputType,
                    Message = $"Invalid version range: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets plugins by module
        /// Demonstrates: Module-based filtering
        /// </summary>
        [HttpGet("plugins/by-module/{moduleName}")]
        public async Task<ActionResult<IEnumerable<PluginInfo>>> GetPluginsByModule(string moduleName)
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
                var module = new NamespaceString(moduleName);
                var plugins = await _jobExecutorFactory.GetPluginsByModuleAsync(module);
                
                var pluginInfos = plugins.Select(p => new PluginInfo
                {
                    Module = p.moduleName.ToString(),
                    Name = p.pluginName.ToString(),
                    Description = p.Description,
                    Version = p.version.ToString()
                }).ToList();

                return Ok(pluginInfos);
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
        /// Gets cache statistics
        /// Demonstrates: Caching layer monitoring
        /// </summary>
        [HttpGet("cache/stats")]
        public ActionResult<CacheStatistics> GetCacheStatistics()
        {
            if (_cachedJobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Cached plugin factory not configured"
                });
            }

            try
            {
                var stats = _cachedJobExecutorFactory.GetCacheStatistics();
                return Ok(stats);
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
        /// Gets circuit breaker status
        /// Demonstrates: Resilience layer monitoring
        /// </summary>
        [HttpGet("circuit-breaker/status")]
        public ActionResult<CircuitBreakerStatus> GetCircuitBreakerStatus()
        {
            if (_resilientJobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Resilient plugin factory not configured"
                });
            }

            try
            {
                var circuitBreaker = _resilientJobExecutorFactory.CircuitBreaker;
                
                return Ok(new CircuitBreakerStatus
                {
                    State = circuitBreaker.State.ToString(),
                    FailureCount = circuitBreaker.FailureCount,
                    IsOpen = circuitBreaker.State == CircuitState.Open,
                    AllowsExecution = circuitBreaker.AllowsExecution,
                    TimeUntilReset = circuitBreaker.TimeUntilReset
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
        /// Resets the circuit breaker
        /// Demonstrates: Manual circuit breaker control
        /// </summary>
        [HttpPost("circuit-breaker/reset")]
        public IActionResult ResetCircuitBreaker()
        {
            if (_resilientJobExecutorFactory == null)
            {
                return BadRequest(new JobError
                {
                    Code = JobErrorCode.ConfigurationError,
                    Message = "Resilient plugin factory not configured"
                });
            }

            _resilientJobExecutorFactory.ResetCircuitBreaker();
            
            return Ok(new { Message = "Circuit breaker reset successfully" });
        }
    }

    /// <summary>
    /// Plugin information DTO
    /// </summary>
    public class PluginInfo
    {
        public string? Module { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public List<string>? Versions { get; set; }
        public string? NewestVersion { get; set; }
    }

    /// <summary>
    /// Cache statistics DTO
    /// </summary>
    public class CacheStatistics
    {
        public int InstanceCacheCount { get; set; }
        public int InstancePoolCount { get; set; }
        public int MetadataCacheCount { get; set; }
        public Dictionary<string, int>? InstancePoolStatistics { get; set; }
    }

    /// <summary>
    /// Circuit breaker status DTO
    /// </summary>
    public class CircuitBreakerStatus
    {
        public string? State { get; set; }
        public int FailureCount { get; set; }
        public bool IsOpen { get; set; }
        public bool AllowsExecution { get; set; }
        public TimeSpan TimeUntilReset { get; set; }
    }

    /// <summary>
    /// Extended job response with additional metadata
    /// </summary>
    public class JobResponse
    {
        public int JobId { get; set; }
        public bool Success { get; set; }
        public string? JobExecutionMessage { get; set; }
        public string? ReturnedJobData { get; set; }
        public bool CacheHit { get; set; }
        public bool ResilientExecution { get; set; }
    }
}
