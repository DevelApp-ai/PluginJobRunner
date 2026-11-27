using System;
using System.Collections.Generic;
using System.Threading;

namespace PluginInterface
{
    /// <summary>
    /// Blessed job execution context contract (decision DevelApp-Repo-Global #89,
    /// derived from Taskhandler TDS §8B.2). Both LaunchMobile-Platform and
    /// Taskhandler pin this single definition: same plugin assemblies run on
    /// both hosts and receive the same context shape.
    ///
    /// The context carries job identity, correlation, scheduling metadata and
    /// host-provided properties (resolved tokens, gateway handles), plus
    /// cancellation and a simple execution log.
    /// </summary>
    public interface IJobExecutionContext
    {
        /// <summary>
        /// Unique id of the job (portable job record id; the host decides representation)
        /// </summary>
        string JobId { get; }

        /// <summary>
        /// Correlation id shared across hosts. A job emitted by one host and
        /// executed by another carries the same value (and the same trace
        /// attribute when instrumented with OpenTelemetry).
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Plugin identity (namespace.className form) the job is dispatched to
        /// </summary>
        string JobExecutorName { get; }

        /// <summary>
        /// Name of the host that emitted the job (e.g. "LaunchMobile-Server").
        /// Null/empty for locally created jobs.
        /// </summary>
        string EmittedBy { get; }

        /// <summary>
        /// Scheduler trigger identity (Quartz trigger key). Null/empty for
        /// manually submitted jobs.
        /// </summary>
        string TriggerId { get; }

        /// <summary>
        /// Scheduling expression (cron or interval) when the job is trigger-driven.
        /// Null/empty for manually submitted jobs.
        /// </summary>
        string TriggerExpression { get; }

        /// <summary>
        /// UTC time the job was enqueued
        /// </summary>
        DateTimeOffset EnqueuedAtUtc { get; }

        /// <summary>
        /// Cancellation token for cooperative cancellation
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Host-provided properties (resolved tokens, gateway handles, inputs).
        /// Never carry secrets in clear text; reference credential store keys instead.
        /// </summary>
        IDictionary<string, string> Properties { get; }

        /// <summary>
        /// Simple execution log; the host decides what to persist
        /// </summary>
        IJobExecutionLog Log { get; }
    }

    /// <summary>
    /// Minimal execution log contract: the plugin appends messages,
    /// the host persists/forwards them (database column, Serilog, telemetry).
    /// </summary>
    public interface IJobExecutionLog
    {
        /// <summary>
        /// Appends a message to the execution log
        /// </summary>
        void Add(string message);

        /// <summary>
        /// Read-only view of the logged messages in order
        /// </summary>
        IReadOnlyList<string> Messages { get; }
    }
}
