using System;

namespace JobRunner.Errors
{
    /// <summary>
    /// Structured error codes for job execution
    /// Provides consistent error categorization and handling
    /// </summary>
    public enum JobErrorCode
    {
        // ========================================================================
        // Success codes (1000-1999)
        // ========================================================================
        
        /// <summary>
        /// Job executed successfully
        /// </summary>
        Success = 1000,

        /// <summary>
        /// Job executed successfully with warnings
        /// </summary>
        SuccessWithWarnings = 1001,

        // ========================================================================
        // Input validation errors (2000-2999)
        // ========================================================================
        
        /// <summary>
        /// Invalid job data
        /// </summary>
        InvalidJobData = 2000,

        /// <summary>
        /// Job data is null or empty
        /// </summary>
        JobDataNullOrEmpty = 2001,

        /// <summary>
        /// Invalid JSON format
        /// </summary>
        InvalidJsonFormat = 2002,

        /// <summary>
        /// Invalid input type
        /// </summary>
        InvalidInputType = 2003,

        /// <summary>
        /// Input array is empty
        /// </summary>
        InputArrayEmpty = 2004,

        /// <summary>
        /// Input contains invalid values
        /// </summary>
        InvalidInputValues = 2005,

        // ========================================================================
        // Authentication and authorization errors (3000-3999)
        // ========================================================================
        
        /// <summary>
        /// Invalid or missing API key
        /// </summary>
        InvalidApiKey = 3000,

        /// <summary>
        /// API key expired
        /// </summary>
        ApiKeyExpired = 3001,

        /// <summary>
        /// Insufficient permissions
        /// </summary>
        InsufficientPermissions = 3002,

        /// <summary>
        /// Rate limit exceeded
        /// </summary>
        RateLimitExceeded = 3003,

        /// <summary>
        /// Access denied
        /// </summary>
        AccessDenied = 3004,

        // ========================================================================
        // Plugin errors (4000-4999)
        // ========================================================================
        
        /// <summary>
        /// Plugin not found
        /// </summary>
        PluginNotFound = 4000,

        /// <summary>
        /// Plugin load failed
        /// </summary>
        PluginLoadFailed = 4001,

        /// <summary>
        /// Plugin instantiation failed
        /// </summary>
        PluginInstantiationFailed = 4002,

        /// <summary>
        /// Plugin execution failed
        /// </summary>
        PluginExecutionFailed = 4003,

        /// <summary>
        /// Plugin version not found
        /// </summary>
        PluginVersionNotFound = 4004,

        /// <summary>
        /// Plugin does not support the requested operation
        /// </summary>
        PluginOperationNotSupported = 4005,

        // ========================================================================
        // Database errors (5000-5999)
        // ========================================================================
        
        /// <summary>
        /// Database connection failed
        /// </summary>
        DatabaseConnectionFailed = 5000,

        /// <summary>
        /// Database query failed
        /// </summary>
        DatabaseQueryFailed = 5001,

        /// <summary>
        /// Database transaction failed
        /// </summary>
        DatabaseTransactionFailed = 5002,

        /// <summary>
        /// Entity not found
        /// </summary>
        EntityNotFound = 5003,

        /// <summary>
        /// Database timeout
        /// </summary>
        DatabaseTimeout = 5004,

        // ========================================================================
        // Scheduler errors (6000-6999)
        // ========================================================================
        
        /// <summary>
        /// Scheduler not running
        /// </summary>
        SchedulerNotRunning = 6000,

        /// <summary>
        /// Job scheduling failed
        /// </summary>
        JobSchedulingFailed = 6001,

        /// <summary>
        /// Job already exists
        /// </summary>
        JobAlreadyExists = 6002,

        /// <summary>
        /// Job not found
        /// </summary>
        JobNotFound = 6003,

        /// <summary>
        /// Invalid schedule expression
        /// </summary>
        InvalidScheduleExpression = 6004,

        // ========================================================================
        // System errors (7000-7999)
        // ========================================================================
        
        /// <summary>
        /// Internal server error
        /// </summary>
        InternalServerError = 7000,

        /// <summary>
        /// Service unavailable
        /// </summary>
        ServiceUnavailable = 7001,

        /// <summary>
        /// Configuration error
        /// </summary>
        ConfigurationError = 7002,

        /// <summary>
        /// Resource exhausted
        /// </summary>
        ResourceExhausted = 7003,

        /// <summary>
        /// Operation timed out
        /// </summary>
        OperationTimeout = 7004,

        /// <summary>
        /// Operation cancelled
        /// </summary>
        OperationCancelled = 7005
    }

    /// <summary>
    /// Job error category
    /// </summary>
    public enum JobErrorCategory
    {
        /// <summary>
        /// Success
        /// </summary>
        Success,

        /// <summary>
        /// Input validation error
        /// </summary>
        InputValidation,

        /// <summary>
        /// Authentication or authorization error
        /// </summary>
        Authentication,

        /// <summary>
        /// Plugin-related error
        /// </summary>
        Plugin,

        /// <summary>
        /// Database-related error
        /// </summary>
        Database,

        /// <summary>
        /// Scheduler-related error
        /// </summary>
        Scheduler,

        /// <summary>
        /// System error
        /// </summary>
        System
    }

    /// <summary>
    /// Job error information
    /// </summary>
    public class JobError
    {
        /// <summary>
        /// Error code
        /// </summary>
        public JobErrorCode Code { get; set; }

        /// <summary>
        /// Error category
        /// </summary>
        public JobErrorCategory Category { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Detailed error description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Exception that caused the error
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Correlation ID for tracking
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Whether this error is retryable
        /// </summary>
        public bool IsRetryable { get; set; }

        /// <summary>
        /// Whether this error is transient
        /// </summary>
        public bool IsTransient { get; set; }

        /// <summary>
        /// Creates a job error
        /// </summary>
        /// <param name="code">Error code</param>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Job error</returns>
        public static JobError Create(
            JobErrorCode code,
            string? message = null,
            Exception? exception = null,
            string? correlationId = null)
        {
            return new JobError
            {
                Code = code,
                Category = GetCategory(code),
                Message = message ?? code.ToString(),
                Exception = exception,
                CorrelationId = correlationId,
                IsRetryable = IsRetryable(code),
                IsTransient = IsTransient(code)
            };
        }

        /// <summary>
        /// Gets the category for an error code
        /// </summary>
        /// <param name="code">Error code</param>
        /// <returns>Error category</returns>
        public static JobErrorCategory GetCategory(JobErrorCode code)
        {
            return code switch
            {
                >= JobErrorCode.Success and <= JobErrorCode.SuccessWithWarnings => JobErrorCategory.Success,
                >= JobErrorCode.InvalidJobData and <= JobErrorCode.InvalidInputValues => JobErrorCategory.InputValidation,
                >= JobErrorCode.InvalidApiKey and <= JobErrorCode.AccessDenied => JobErrorCategory.Authentication,
                >= JobErrorCode.PluginNotFound and <= JobErrorCode.PluginOperationNotSupported => JobErrorCategory.Plugin,
                >= JobErrorCode.DatabaseConnectionFailed and <= JobErrorCode.DatabaseTimeout => JobErrorCategory.Database,
                >= JobErrorCode.SchedulerNotRunning and <= JobErrorCode.InvalidScheduleExpression => JobErrorCategory.Scheduler,
                _ => JobErrorCategory.System
            };
        }

        /// <summary>
        /// Checks if an error code is retryable
        /// </summary>
        /// <param name="code">Error code</param>
        /// <returns>True if retryable</returns>
        public static bool IsRetryable(JobErrorCode code)
        {
            return code switch
            {
                JobErrorCode.RateLimitExceeded => false,
                JobErrorCode.InvalidApiKey => false,
                JobErrorCode.AccessDenied => false,
                JobErrorCode.PluginNotFound => false,
                JobErrorCode.PluginOperationNotSupported => false,
                JobErrorCode.EntityNotFound => false,
                JobErrorCode.InvalidScheduleExpression => false,
                JobErrorCode.ConfigurationError => false,
                _ => true
            };
        }

        /// <summary>
        /// Checks if an error code is transient
        /// </summary>
        /// <param name="code">Error code</param>
        /// <returns>True if transient</returns>
        public static bool IsTransient(JobErrorCode code)
        {
            return code switch
            {
                JobErrorCode.Success => false,
                JobErrorCode.SuccessWithWarnings => false,
                JobErrorCode.InvalidJobData => false,
                JobErrorCode.JobDataNullOrEmpty => false,
                JobErrorCode.InvalidJsonFormat => false,
                JobErrorCode.InvalidInputType => false,
                JobErrorCode.InvalidInputValues => false,
                JobErrorCode.InvalidApiKey => false,
                JobErrorCode.ApiKeyExpired => false,
                JobErrorCode.InsufficientPermissions => false,
                JobErrorCode.AccessDenied => false,
                JobErrorCode.PluginNotFound => false,
                JobErrorCode.PluginOperationNotSupported => false,
                JobErrorCode.EntityNotFound => false,
                JobErrorCode.JobAlreadyExists => false,
                JobErrorCode.JobNotFound => false,
                JobErrorCode.InvalidScheduleExpression => false,
                JobErrorCode.ConfigurationError => false,
                _ => true
            };
        }

        public override string ToString()
        {
            return $"[{(int)Code} {Code}: {Message}]";
        }
    }

    /// <summary>
    /// Exception for job execution errors
    /// </summary>
    public class JobExecutionException : Exception
    {
        /// <summary>
        /// Error code
        /// </summary>
        public JobErrorCode ErrorCode { get; }

        /// <summary>
        /// Error information
        /// </summary>
        public JobError Error { get; }

        /// <summary>
        /// Creates a job execution exception
        /// </summary>
        /// <param name="errorCode">Error code</param>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        /// <param name="correlationId">Correlation ID</param>
        public JobExecutionException(
            JobErrorCode errorCode,
            string? message = null,
            Exception? innerException = null,
            string? correlationId = null)
            : base(message ?? errorCode.ToString(), innerException)
        {
            ErrorCode = errorCode;
            Error = JobError.Create(errorCode, message, innerException, correlationId);
        }

        /// <summary>
        /// Creates a job execution exception from a job error
        /// </summary>
        /// <param name="error">Job error</param>
        public JobExecutionException(JobError error)
            : base(error.Message, error.Exception)
        {
            ErrorCode = error.Code;
            Error = error;
        }
    }

    /// <summary>
    /// Event arguments for job errors
    /// </summary>
    public class JobErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Error information
        /// </summary>
        public JobError Error { get; set; } = new JobError();

        /// <summary>
        /// Job ID
        /// </summary>
        public Guid? JobId { get; set; }

        /// <summary>
        /// Plugin name
        /// </summary>
        public string? PluginName { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
