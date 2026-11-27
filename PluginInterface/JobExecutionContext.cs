using System;
using System.Collections.Generic;
using System.Threading;

namespace PluginInterface
{
    /// <summary>
    /// Default concrete implementation of IJobExecutionContext.
    /// Hosts may provide their own implementation; this one is sufficient
    /// for the JobRunner and for unit tests of plugin executors.
    /// </summary>
    public class JobExecutionContext : IJobExecutionContext
    {
        private readonly List<string> _messages = new List<string>();
        private IJobExecutionLog _log;

        public JobExecutionContext()
        {
        }

        public JobExecutionContext(
            string jobId,
            string correlationId,
            string jobExecutorName,
            string emittedBy = null,
            string triggerId = null,
            string triggerExpression = null,
            DateTimeOffset? enqueuedAtUtc = null,
            CancellationToken cancellationToken = default)
        {
            JobId = jobId;
            CorrelationId = correlationId;
            JobExecutorName = jobExecutorName;
            EmittedBy = emittedBy;
            TriggerId = triggerId;
            TriggerExpression = triggerExpression;
            EnqueuedAtUtc = enqueuedAtUtc ?? DateTimeOffset.UtcNow;
            CancellationToken = cancellationToken;
        }

        public string JobId { get; set; }

        public string CorrelationId { get; set; }

        public string JobExecutorName { get; set; }

        public string EmittedBy { get; set; }

        public string TriggerId { get; set; }

        public string TriggerExpression { get; set; }

        public DateTimeOffset EnqueuedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public IDictionary<string, string> Properties { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public IJobExecutionLog Log
        {
            get { return _log ?? (_log = new DefaultJobExecutionLog(_messages)); }
        }

        private sealed class DefaultJobExecutionLog : IJobExecutionLog
        {
            private readonly List<string> _messages;

            public DefaultJobExecutionLog(List<string> messages)
            {
                _messages = messages;
            }

            public void Add(string message)
            {
                if (message != null)
                {
                    _messages.Add(message);
                }
            }

            public IReadOnlyList<string> Messages
            {
                get { return _messages; }
            }
        }
    }
}
