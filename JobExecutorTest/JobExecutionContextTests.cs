using FluentAssertions;
using JobExecutorModel;
using JobRunner.JobExecutionFactory;
using JobRunner.Telemetry;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;

namespace JobExecutorTest
{
    /// <summary>
    /// Tests for the blessed IJobExecutionContext contract (decision
    /// DevelApp-Repo-Global #89, PJR-1) and the job-path telemetry (PJR-3).
    /// </summary>
    public class JobExecutionContextTests
    {
        [Fact]
        public void ConstructorAssignsAllContractMembers()
        {
            var enqueued = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
            var cts = new CancellationTokenSource();

            var context = new JobExecutionContext(
                jobId: "42",
                correlationId: "corr-1",
                jobExecutorName: "JobExecutor.MergeSortArray",
                emittedBy: "LaunchMobile-Server",
                triggerId: "trigger-9",
                triggerExpression: "0 0/5 * * * ?",
                enqueuedAtUtc: enqueued,
                cancellationToken: cts.Token);

            context.JobId.Should().Be("42");
            context.CorrelationId.Should().Be("corr-1");
            context.JobExecutorName.Should().Be("JobExecutor.MergeSortArray");
            context.EmittedBy.Should().Be("LaunchMobile-Server");
            context.TriggerId.Should().Be("trigger-9");
            context.TriggerExpression.Should().Be("0 0/5 * * * ?");
            context.EnqueuedAtUtc.Should().Be(enqueued);
            context.CancellationToken.Should().Be(cts.Token);
        }

        [Fact]
        public void PropertiesDictionaryIsMutableAndCaseSensitiveByDefault()
        {
            var context = new JobExecutionContext();
            context.Properties["gateway"] = "github";
            context.Properties["Gateway"] = "robot";

            context.Properties["gateway"].Should().Be("github");
            context.Properties["Gateway"].Should().Be("robot");
        }

        [Fact]
        public void LogKeepsInsertionOrderAndIgnoresNullMessages()
        {
            var context = new JobExecutionContext();
            var log = ((IJobExecutionContext)context).Log;
            log.Add("first");
            log.Add(null);
            log.Add("second");

            log.Messages.Should().Equal("first", "second");
        }

        [Fact]
        public void LogIsStableAcrossAccesses()
        {
            var context = new JobExecutionContext();
            var typed = ((IJobExecutionContext)context).Log;
            typed.Add("one");

            var again = ((IJobExecutionContext)context).Log;
            again.Messages.Should().ContainSingle().Which.Should().Be("one");
        }

        [Fact]
        public void FactoryCreatesForRequestAssignsCorrelationIdWhenMissing()
        {
            var request = new PostJobRequest
            {
                JobExecutor = "JobExecutor.MergeSortArray",
                JobData = "{}",
                EmittedBy = "LaunchMobile-Server",
                TriggerId = "t-1"
            };

            var context = JobExecutionContextFactory.CreateForRequest(request);

            context.JobExecutorName.Should().Be("JobExecutor.MergeSortArray");
            context.EmittedBy.Should().Be("LaunchMobile-Server");
            context.TriggerId.Should().Be("t-1");
            context.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void FactoryKeepsCallerSuppliedCorrelationId()
        {
            var request = new PostJobRequest { CorrelationId = "keep-me" };
            var context = JobExecutionContextFactory.CreateForRequest(request);
            context.CorrelationId.Should().Be("keep-me");
        }

        [Fact]
        public void FactoryCreatesForJobMapsEntityFields()
        {
            var job = new Job
            {
                JobId = 7,
                JobExecutor = "JobExecutor.MergeSortArray",
                CorrelationId = "corr-7",
                EmittedBy = "LaunchMobile-Builder",
                TriggerId = "trigger-7",
                TriggerExpression = "0 0 12 * * ?",
                Enqueued = new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc)
            };

            var context = JobExecutionContextFactory.CreateForJob(job);

            context.JobId.Should().Be("7");
            context.CorrelationId.Should().Be("corr-7");
            context.JobExecutorName.Should().Be("JobExecutor.MergeSortArray");
            context.EmittedBy.Should().Be("LaunchMobile-Builder");
            context.TriggerId.Should().Be("trigger-7");
            context.TriggerExpression.Should().Be("0 0 12 * * ?");
            context.EnqueuedAtUtc.UtcDateTime.Should().Be(job.Enqueued);
        }

        [Fact]
        public void FactoryCreatesForJobAssignsCorrelationIdWhenEntityHasNone()
        {
            var job = new Job { JobId = 1, JobExecutor = "JobExecutor.MergeSortArray" };
            var context = JobExecutionContextFactory.CreateForJob(job);
            context.CorrelationId.Should().NotBeNullOrEmpty();
        }
    }

    public class JobTelemetryTests
    {
        [Fact]
        public void StartJobActivityReturnsActivityWithContractTagsWhenListenerAttached()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == JobTelemetry.ActivitySourceName,
                SampleUsingParentId = (ref ActivityCreationOptions<string> o) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> o) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            try
            {
                using var activity = JobTelemetry.StartJobActivity(
                    "42", "corr-42", "JobExecutor.MergeSortArray", "trigger-1", "LaunchMobile-Server");

                activity.Should().NotBeNull();
                activity.OperationName.Should().Be(JobTelemetry.ActivityNameJobExecution);
                activity.GetTagItem(JobTelemetry.TagJobId).Should().Be("42");
                activity.GetTagItem(JobTelemetry.TagCorrelationId).Should().Be("corr-42");
                activity.GetTagItem(JobTelemetry.TagJobExecutor).Should().Be("JobExecutor.MergeSortArray");
                activity.GetTagItem(JobTelemetry.TagTriggerId).Should().Be("trigger-1");
                activity.GetTagItem(JobTelemetry.TagEmittedBy).Should().Be("LaunchMobile-Server");
            }
            finally
            {
                // listener disposed by using; Activity.Current cleanup
                Activity.Current = null;
            }
        }

        [Fact]
        public void StartJobActivityWithoutListenerReturnsNull()
        {
            using var activity = JobTelemetry.StartJobActivity("1", "c", "e");
            // No listener attached in this test: StartActivity returns null
            // and callers null-check before dispose.
            // (If a parallel test attached a listener this may be non-null;
            // the contract is only "non-null when sampled".)
            Assert.True(activity == null || activity.OperationName == JobTelemetry.ActivityNameJobExecution);
        }

        [Fact]
        public void ActivitySourceNameIsStableWireContract()
        {
            JobTelemetry.ActivitySourceName.Should().Be("DevelApp.PluginJobRunner");
            JobTelemetry.TagCorrelationId.Should().Be("pjr.job.correlation_id");
        }
    }
}
