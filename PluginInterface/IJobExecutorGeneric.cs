using DevelApp.RuntimePluggableClassFactory.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PluginInterface
{
    /// <summary>
    /// Generic, type-safe interface for job execution
    /// Provides strongly-typed input and output for better type safety
    /// </summary>
    /// <typeparam name="TInput">Type of input data for the job</typeparam>
    /// <typeparam name="TOutput">Type of output data from the job</typeparam>
    public interface IJobExecutor<TInput, TOutput> : IPluginClass, ITypedPluginClass<TInput, TOutput>
    {
        /// <summary>
        /// Executes the job with strongly-typed input and output
        /// </summary>
        /// <param name="context">Execution context providing logging and cancellation support</param>
        /// <param name="input">Strongly-typed input data</param>
        /// <returns>Execution result with success status, output data, and error information</returns>
        Task<JobExecutionResult<TOutput>> ExecuteAsync(
            IJobExecutionContext? context,
            TInput input,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Default bridge to the ITypedPluginClass contract (required by
        /// TypedPluginClassFactory&lt;TPlugin, TInput, TOutput&gt;). Implemented as a
        /// default interface method so every existing IJobExecutor plugin satisfies
        /// the typed contract without adding an Execute method per plugin.
        /// </summary>
        PluginExecutionResult<TOutput> ITypedPluginClass<TInput, TOutput>.Execute(
            IPluginExecutionContext? context,
            TInput input)
        {
            var result = ExecuteAsync(
                null,
                input,
                context?.CancellationToken ?? CancellationToken.None).GetAwaiter().GetResult();

            return result.Success
                ? PluginExecutionResult<TOutput>.CreateSuccess(result.Data)
                : PluginExecutionResult<TOutput>.CreateFailure(
                    result.ErrorMessage ?? "Job execution failed",
                    result.Exception);
        }
    }

    /// <summary>
    /// Result of a job execution
    /// </summary>
    /// <typeparam name="TOutput">Type of the output data</typeparam>
    public class JobExecutionResult<TOutput>
    {
        /// <summary>
        /// Indicates if the execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The output data from the job (only valid if Success is true)
        /// </summary>
        public TOutput? Data { get; set; }

        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Exception that caused the failure (if any)
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Execution duration
        /// </summary>
        public TimeSpan ExecutionDuration { get; set; }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        /// <param name="data">The output data</param>
        /// <param name="executionDuration">Execution duration</param>
        /// <returns>Successful execution result</returns>
        public static JobExecutionResult<TOutput> CreateSuccess(
            TOutput data,
            TimeSpan executionDuration)
        {
            return new JobExecutionResult<TOutput>
            {
                Success = true,
                Data = data,
                ExecutionDuration = executionDuration
            };
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="exception">Optional exception</param>
        /// <param name="executionDuration">Execution duration</param>
        /// <returns>Failed execution result</returns>
        public static JobExecutionResult<TOutput> CreateFailure(
            string errorMessage,
            Exception? exception = null,
            TimeSpan executionDuration = default)
        {
            return new JobExecutionResult<TOutput>
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                ExecutionDuration = executionDuration
            };
        }
    }

    /// <summary>
    /// DTO for merge sort array job input
    /// </summary>
    public class MergeSortInput
    {
        /// <summary>
        /// Array of integers to sort
        /// </summary>
        public int[]? Values { get; set; }
    }

    /// <summary>
    /// DTO for merge sort array job output
    /// </summary>
    public class MergeSortOutput
    {
        /// <summary>
        /// Sorted array of integers
        /// </summary>
        public int[]? SortedValues { get; set; }

        /// <summary>
        /// Original array length
        /// </summary>
        public int OriginalLength { get; set; }

        /// <summary>
        /// Sorting algorithm used
        /// </summary>
        public string Algorithm { get; set; } = "MergeSort";
    }
}
