using System;
using FluentValidation;
using JobExecutorModel;

namespace JobRunner.Validation
{
    /// <summary>
    /// Validator for Job entity
    /// </summary>
    public class JobValidator : AbstractValidator<Job>
    {
        public JobValidator()
        {
            RuleFor(x => x.JobExecutor)
                .NotEmpty().WithMessage("JobExecutor is required")
                .MaximumLength(100).WithMessage("JobExecutor must be 100 characters or less");

            RuleFor(x => x.JobData)
                .MaximumLength(10000).WithMessage("JobData must be 10,000 characters or less");

            RuleFor(x => x.JobExecutionMessage)
                .MaximumLength(2000).WithMessage("JobExecutionMessage must be 2,000 characters or less");

            RuleFor(x => x.JobExecutionDuration)
                .GreaterThanOrEqualTo(TimeSpan.Zero).WithMessage("JobExecutionDuration cannot be negative");
        }
    }
}
