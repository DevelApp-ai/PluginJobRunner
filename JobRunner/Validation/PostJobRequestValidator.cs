using FluentValidation;
using JobExecutorModel;

namespace JobRunner.Validation
{
    /// <summary>
    /// Validator for PostJobRequest DTO
    /// </summary>
    public class PostJobRequestValidator : AbstractValidator<PostJobRequest>
    {
        public PostJobRequestValidator()
        {
            RuleFor(x => x.JobExecutor)
                .NotEmpty().WithMessage("JobExecutor is required")
                .MaximumLength(100).WithMessage("JobExecutor must be 100 characters or less");

            RuleFor(x => x.JobData)
                .NotEmpty().WithMessage("JobData is required")
                .MaximumLength(10000).WithMessage("JobData must be 10,000 characters or less");
        }
    }
}
