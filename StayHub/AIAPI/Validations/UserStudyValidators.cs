using AIAPI.DTOs;
using FluentValidation;
using StayHub.Common.Localization;

namespace AIAPI.Validations;

public class SubmitUserStudyResponseValidator : AbstractValidator<SubmitUserStudyResponseDTO>
{
    public SubmitUserStudyResponseValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.AssignmentId).GreaterThan(0).WithMessage(v.Get("AssignmentId must be greater than 0."));
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage(v.Get("SessionId is required."))
            .MinimumLength(8).WithMessage(v.Get("SessionId must be between 8 and 64 characters."))
            .MaximumLength(64).WithMessage(v.Get("SessionId must be between 8 and 64 characters."));
        RuleFor(x => x.PreferredList).NotEmpty().WithMessage(v.Get("PreferredList is required."));

        RuleFor(x => x.FairnessListA).InclusiveBetween(1, 7).WithMessage(v.Get("Rating must be between 1 and 7."));
        RuleFor(x => x.FairnessListB).InclusiveBetween(1, 7).WithMessage(v.Get("Rating must be between 1 and 7."));
        RuleFor(x => x.SatisfactionListA).InclusiveBetween(1, 7).WithMessage(v.Get("Rating must be between 1 and 7."));
        RuleFor(x => x.SatisfactionListB).InclusiveBetween(1, 7).WithMessage(v.Get("Rating must be between 1 and 7."));
        RuleFor(x => x.GroupFairnessListA).InclusiveBetween(1, 7).WithMessage(v.Get("Rating must be between 1 and 7."));
        RuleFor(x => x.GroupFairnessListB).InclusiveBetween(1, 7).WithMessage(v.Get("Rating must be between 1 and 7."));
        RuleFor(x => x.WouldBookListA).InclusiveBetween(1, 7).WithMessage(v.Get("Rating must be between 1 and 7."));
        RuleFor(x => x.WouldBookListB).InclusiveBetween(1, 7).WithMessage(v.Get("Rating must be between 1 and 7."));

        RuleFor(x => x.OpenComment).MaximumLength(500).WithMessage(v.Get("Comment cannot exceed 500 characters."))
            .When(x => !string.IsNullOrWhiteSpace(x.OpenComment));
        RuleFor(x => x.AgeGroup).MaximumLength(20).WithMessage(v.Get("AgeGroup cannot exceed 20 characters."))
            .When(x => !string.IsNullOrWhiteSpace(x.AgeGroup));
        RuleFor(x => x.TravelExperience).MaximumLength(30).WithMessage(v.Get("TravelExperience cannot exceed 30 characters."))
            .When(x => !string.IsNullOrWhiteSpace(x.TravelExperience));
    }
}
