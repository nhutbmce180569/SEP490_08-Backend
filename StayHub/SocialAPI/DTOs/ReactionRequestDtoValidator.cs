using FluentValidation;
using StayHub.Common.Localization;

namespace SocialAPI.DTOs;

public class ReactionRequestDtoValidator : AbstractValidator<ReactionRequestDto>
{
    public ReactionRequestDtoValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.MomentId)
            .GreaterThan(0).WithMessage(v.Get("MomentId must be greater than 0."));

        RuleFor(x => x.IsLike)
            .NotNull().WithMessage(v.Get("IsLike cannot be null."));
    }
}
