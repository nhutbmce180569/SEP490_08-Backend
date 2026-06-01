using AIAPI.DTOs;
using FluentValidation;

namespace AIAPI.Validations;

public class SubmitUserStudyResponseValidator : AbstractValidator<SubmitUserStudyResponseDTO>
{
    public SubmitUserStudyResponseValidator()
    {
        RuleFor(x => x.AssignmentId).GreaterThan(0);
        RuleFor(x => x.SessionId).NotEmpty().MinimumLength(8).MaximumLength(64);
        RuleFor(x => x.PreferredList).NotEmpty();

        RuleFor(x => x.FairnessListA).InclusiveBetween(1, 7);
        RuleFor(x => x.FairnessListB).InclusiveBetween(1, 7);
        RuleFor(x => x.SatisfactionListA).InclusiveBetween(1, 7);
        RuleFor(x => x.SatisfactionListB).InclusiveBetween(1, 7);
        RuleFor(x => x.GroupFairnessListA).InclusiveBetween(1, 7);
        RuleFor(x => x.GroupFairnessListB).InclusiveBetween(1, 7);
        RuleFor(x => x.WouldBookListA).InclusiveBetween(1, 7);
        RuleFor(x => x.WouldBookListB).InclusiveBetween(1, 7);

        RuleFor(x => x.OpenComment).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.OpenComment));
        RuleFor(x => x.AgeGroup).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.AgeGroup));
        RuleFor(x => x.TravelExperience).MaximumLength(30).When(x => !string.IsNullOrWhiteSpace(x.TravelExperience));
    }
}
